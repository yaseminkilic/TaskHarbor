using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc;
using TaskTracker.Api.Endpoints;
using TaskTracker.Application.Projects.Dtos;
using TaskTracker.Application.Tasks.Dtos;
using TaskTracker.Application.Users.Dtos;
using TaskTracker.IntegrationTests.Infrastructure;

namespace TaskTracker.IntegrationTests.Endpoints;

public class TaskEndpointsTests : IClassFixture<CustomWebApplicationFactory>, IAsyncLifetime
{
    private readonly CustomWebApplicationFactory _factory;
    private readonly HttpClient _client;

    public TaskEndpointsTests(CustomWebApplicationFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
    }

    public Task InitializeAsync() => _factory.ResetDatabaseAsync();
    public Task DisposeAsync() => Task.CompletedTask;

    // ---------- seed helpers ----------

    private async Task<ProjectDto> SeedProjectAsync()
    {
        var userResp = await _client.PostAsJsonAsync("/users", new CreateUserRequest("alice@example.com", "Alice"));
        userResp.EnsureSuccessStatusCode();
        var owner = (await userResp.Content.ReadFromJsonAsync<UserDto>())!;

        var projResp = await _client.PostAsJsonAsync("/projects", new CreateProjectRequest(owner.Id, "Apollo"));
        projResp.EnsureSuccessStatusCode();
        return (await projResp.Content.ReadFromJsonAsync<ProjectDto>())!;
    }

    private async Task<TaskDto> SeedTaskAsync(Guid projectId, string title = "Write tests", string? description = null)
    {
        var resp = await _client.PostAsJsonAsync(
            $"/projects/{projectId}/tasks",
            new TaskEndpoints.CreateTaskBody(title, description));
        resp.EnsureSuccessStatusCode();
        return (await resp.Content.ReadFromJsonAsync<TaskDto>())!;
    }

    // ---------- POST /projects/{id}/tasks ----------

    // Regression for Adım 7 bug #1, end-to-end through HTTP.
    [Fact]
    public async Task Post_CreatesTask_Returns201_WithOpenStatus_AndIsRetrievable()
    {
        var project = await SeedProjectAsync();

        var resp = await _client.PostAsJsonAsync(
            $"/projects/{project.Id}/tasks",
            new TaskEndpoints.CreateTaskBody("Write tests", "for the API"));

        resp.StatusCode.Should().Be(HttpStatusCode.Created);
        resp.Headers.Location!.ToString().Should().StartWith("/tasks/");

        var created = await resp.Content.ReadFromJsonAsync<TaskDto>();
        created.Should().NotBeNull();
        created!.Status.Should().Be("Open");
        created.ProjectId.Should().Be(project.Id);

        // Round-trip through GET to prove the row actually landed in the DB
        var fetched = await _client.GetFromJsonAsync<TaskDto>($"/tasks/{created.Id}");
        fetched.Should().NotBeNull();
        fetched!.Title.Should().Be("Write tests");
    }

    [Fact]
    public async Task Post_ProjectNotFound_Returns404()
    {
        var resp = await _client.PostAsJsonAsync(
            $"/projects/{Guid.NewGuid()}/tasks",
            new TaskEndpoints.CreateTaskBody("t", null));

        resp.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public async Task Post_EmptyTitle_Returns400(string title)
    {
        var project = await SeedProjectAsync();

        var resp = await _client.PostAsJsonAsync(
            $"/projects/{project.Id}/tasks",
            new TaskEndpoints.CreateTaskBody(title, null));

        resp.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    // ---------- GET /tasks/{id} ----------

    [Fact]
    public async Task Get_ById_NotFound_Returns404()
    {
        var resp = await _client.GetAsync($"/tasks/{Guid.NewGuid()}");

        resp.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    // ---------- PATCH /tasks/{id} ----------

    [Fact]
    public async Task Patch_UpdatesTitleAndDescription_Returns200()
    {
        var project = await SeedProjectAsync();
        var task = await SeedTaskAsync(project.Id, "old", "old desc");

        var resp = await _client.PatchAsJsonAsync($"/tasks/{task.Id}", new UpdateTaskRequest("new title", "new desc"));

        resp.StatusCode.Should().Be(HttpStatusCode.OK);
        var updated = await resp.Content.ReadFromJsonAsync<TaskDto>();
        updated!.Title.Should().Be("new title");
        updated.Description.Should().Be("new desc");
    }

    [Fact]
    public async Task Patch_AfterComplete_Returns400_DomainRuleViolation()
    {
        var project = await SeedProjectAsync();
        var task = await SeedTaskAsync(project.Id);
        await _client.PostAsync($"/tasks/{task.Id}/complete", null);

        var resp = await _client.PatchAsJsonAsync($"/tasks/{task.Id}", new UpdateTaskRequest("new", null));

        resp.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        var problem = await resp.Content.ReadFromJsonAsync<ProblemDetails>();
        problem!.Title.Should().Be("Domain rule violation");
    }

    // ---------- POST /tasks/{id}/start ----------

    [Fact]
    public async Task Post_Start_Returns200_InProgress()
    {
        var project = await SeedProjectAsync();
        var task = await SeedTaskAsync(project.Id);

        var resp = await _client.PostAsync($"/tasks/{task.Id}/start", null);

        resp.StatusCode.Should().Be(HttpStatusCode.OK);
        var dto = await resp.Content.ReadFromJsonAsync<TaskDto>();
        dto!.Status.Should().Be("InProgress");
    }

    [Fact]
    public async Task Post_Start_AfterDone_Returns400()
    {
        var project = await SeedProjectAsync();
        var task = await SeedTaskAsync(project.Id);
        await _client.PostAsync($"/tasks/{task.Id}/complete", null);

        var resp = await _client.PostAsync($"/tasks/{task.Id}/start", null);

        resp.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    // ---------- POST /tasks/{id}/complete ----------

    [Fact]
    public async Task Post_Complete_Returns200_Done_WithTimestamp()
    {
        var project = await SeedProjectAsync();
        var task = await SeedTaskAsync(project.Id);

        var resp = await _client.PostAsync($"/tasks/{task.Id}/complete", null);

        resp.StatusCode.Should().Be(HttpStatusCode.OK);
        var dto = await resp.Content.ReadFromJsonAsync<TaskDto>();
        dto!.Status.Should().Be("Done");
        dto.CompletedAtUtc.Should().NotBeNull();
    }

    // Regression for Adım 7 bug #2, end-to-end through HTTP.
    [Fact]
    public async Task Post_Complete_PreservesTags_InResponse()
    {
        var project = await SeedProjectAsync();
        var task = await SeedTaskAsync(project.Id);
        await _client.PostAsJsonAsync($"/tasks/{task.Id}/tags", new TaskEndpoints.AddTagBody("urgent"));

        var resp = await _client.PostAsync($"/tasks/{task.Id}/complete", null);

        resp.StatusCode.Should().Be(HttpStatusCode.OK);
        var dto = await resp.Content.ReadFromJsonAsync<TaskDto>();
        dto!.Status.Should().Be("Done");
        dto.Tags.Should().ContainSingle().Which.Should().Be("urgent");
    }

    // ---------- DELETE /tasks/{id} ----------

    [Fact]
    public async Task Delete_Returns204_AndTaskGone()
    {
        var project = await SeedProjectAsync();
        var task = await SeedTaskAsync(project.Id);

        var delResp = await _client.DeleteAsync($"/tasks/{task.Id}");
        delResp.StatusCode.Should().Be(HttpStatusCode.NoContent);

        var getResp = await _client.GetAsync($"/tasks/{task.Id}");
        getResp.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Delete_NotFound_Returns404()
    {
        var resp = await _client.DeleteAsync($"/tasks/{Guid.NewGuid()}");

        resp.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    // ---------- GET /projects/{id}/tasks?status= ----------

    [Fact]
    public async Task Get_ListByProject_FiltersByStatus_CaseInsensitive()
    {
        var project = await SeedProjectAsync();
        var a = await SeedTaskAsync(project.Id, "a");
        var b = await SeedTaskAsync(project.Id, "b");
        await _client.PostAsync($"/tasks/{a.Id}/start", null);
        await _client.PostAsync($"/tasks/{b.Id}/complete", null);

        var list = await _client.GetFromJsonAsync<List<TaskDto>>($"/projects/{project.Id}/tasks?status=inprogress");

        list.Should().ContainSingle().Which.Id.Should().Be(a.Id);
    }

    // Regression for the status filter parse error path (Adım 7 plan'da hit edilmedi).
    [Fact]
    public async Task Get_ListByProject_BogusStatus_Returns400_ValidationProblem()
    {
        var project = await SeedProjectAsync();

        var resp = await _client.GetAsync($"/projects/{project.Id}/tasks?status=Bogus");

        resp.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        var problem = await resp.Content.ReadFromJsonAsync<ValidationProblemDetails>();
        problem!.Errors.Should().ContainKey("status");
    }

    // ---------- POST /tasks/{id}/tags ----------

    [Fact]
    public async Task Post_Tags_AttachTag_Returns200_AndIsNormalized()
    {
        var project = await SeedProjectAsync();
        var task = await SeedTaskAsync(project.Id);

        var resp = await _client.PostAsJsonAsync(
            $"/tasks/{task.Id}/tags",
            new TaskEndpoints.AddTagBody("  Urgent  "));

        resp.StatusCode.Should().Be(HttpStatusCode.OK);
        var dto = await resp.Content.ReadFromJsonAsync<TaskDto>();
        dto!.Tags.Should().ContainSingle().Which.Should().Be("urgent");
    }

    // ---------- DELETE /tasks/{id}/tags/{name} ----------

    [Fact]
    public async Task Delete_Tag_DetachesTag_Returns200()
    {
        var project = await SeedProjectAsync();
        var task = await SeedTaskAsync(project.Id);
        await _client.PostAsJsonAsync($"/tasks/{task.Id}/tags", new TaskEndpoints.AddTagBody("urgent"));

        var resp = await _client.DeleteAsync($"/tasks/{task.Id}/tags/URGENT");

        resp.StatusCode.Should().Be(HttpStatusCode.OK);
        var dto = await resp.Content.ReadFromJsonAsync<TaskDto>();
        dto!.Tags.Should().BeEmpty();
    }
}
