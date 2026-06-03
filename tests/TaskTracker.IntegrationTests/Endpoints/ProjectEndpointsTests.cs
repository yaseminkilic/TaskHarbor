using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using TaskTracker.Application.Projects.Dtos;
using TaskTracker.Application.Users.Dtos;
using TaskTracker.IntegrationTests.Infrastructure;

namespace TaskTracker.IntegrationTests.Endpoints;

public class ProjectEndpointsTests : IClassFixture<CustomWebApplicationFactory>, IAsyncLifetime
{
    private readonly CustomWebApplicationFactory _factory;
    private readonly HttpClient _client;

    public ProjectEndpointsTests(CustomWebApplicationFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
    }

    public Task InitializeAsync() => _factory.ResetDatabaseAsync();
    public Task DisposeAsync() => Task.CompletedTask;

    private async Task<UserDto> CreateUserAsync(string email = "alice@example.com", string name = "Alice")
    {
        var response = await _client.PostAsJsonAsync("/users", new CreateUserRequest(email, name));
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<UserDto>())!;
    }

    [Fact]
    public async Task Post_CreatesProject_AndReturns201()
    {
        var owner = await CreateUserAsync();

        var response = await _client.PostAsJsonAsync("/projects", new CreateProjectRequest(owner.Id, "Apollo"));

        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var dto = await response.Content.ReadFromJsonAsync<ProjectDto>();
        dto.Should().NotBeNull();
        dto!.Name.Should().Be("Apollo");
        dto.OwnerId.Should().Be(owner.Id);
        dto.TaskCount.Should().Be(0);
    }

    [Fact]
    public async Task Post_OwnerNotFound_Returns404()
    {
        var response = await _client.PostAsJsonAsync("/projects", new CreateProjectRequest(Guid.NewGuid(), "Apollo"));

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public async Task Post_InvalidName_Returns400(string name)
    {
        var owner = await CreateUserAsync();

        var response = await _client.PostAsJsonAsync("/projects", new CreateProjectRequest(owner.Id, name));

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Get_ById_NotFound_Returns404()
    {
        var response = await _client.GetAsync($"/projects/{Guid.NewGuid()}");

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Get_ByOwner_ReturnsOnlyOwnerProjects()
    {
        var alice = await CreateUserAsync("alice@example.com", "Alice");
        var bob = await CreateUserAsync("bob@example.com", "Bob");
        await _client.PostAsJsonAsync("/projects", new CreateProjectRequest(alice.Id, "Alice P"));
        await _client.PostAsJsonAsync("/projects", new CreateProjectRequest(bob.Id, "Bob P"));

        var list = await _client.GetFromJsonAsync<List<ProjectDto>>($"/users/{alice.Id}/projects");

        list.Should().ContainSingle().Which.Name.Should().Be("Alice P");
    }
}
