using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using TaskTracker.Application.Users.Dtos;
using TaskTracker.IntegrationTests.Infrastructure;

namespace TaskTracker.IntegrationTests.Endpoints;

public class UserEndpointsTests : IClassFixture<CustomWebApplicationFactory>, IAsyncLifetime
{
    private readonly CustomWebApplicationFactory _factory;
    private readonly HttpClient _client;

    public UserEndpointsTests(CustomWebApplicationFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
    }

    public Task InitializeAsync() => _factory.ResetDatabaseAsync();
    public Task DisposeAsync() => Task.CompletedTask;

    [Fact]
    public async Task Post_CreatesUser_AndReturns201()
    {
        var response = await _client.PostAsJsonAsync("/users", new CreateUserRequest("alice@example.com", "Alice"));

        response.StatusCode.Should().Be(HttpStatusCode.Created);
        response.Headers.Location!.ToString().Should().StartWith("/users/");

        var dto = await response.Content.ReadFromJsonAsync<UserDto>();
        dto.Should().NotBeNull();
        dto!.Email.Should().Be("alice@example.com");
        dto.Id.Should().NotBe(Guid.Empty);
    }

    [Fact]
    public async Task Post_DuplicateEmail_Returns409()
    {
        await _client.PostAsJsonAsync("/users", new CreateUserRequest("alice@example.com", "Alice"));

        var second = await _client.PostAsJsonAsync("/users", new CreateUserRequest("ALICE@example.com", "Other"));

        second.StatusCode.Should().Be(HttpStatusCode.Conflict);
    }

    [Theory]
    [InlineData("", "Alice")]
    [InlineData("not-an-email", "Alice")]
    [InlineData("alice@example.com", "")]
    public async Task Post_InvalidRequest_Returns400(string email, string displayName)
    {
        var response = await _client.PostAsJsonAsync("/users", new CreateUserRequest(email, displayName));

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Get_ById_NotFound_Returns404()
    {
        var response = await _client.GetAsync($"/users/{Guid.NewGuid()}");

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Get_List_ReturnsCreatedUsers()
    {
        await _client.PostAsJsonAsync("/users", new CreateUserRequest("a@example.com", "A"));
        await _client.PostAsJsonAsync("/users", new CreateUserRequest("b@example.com", "B"));

        var list = await _client.GetFromJsonAsync<List<UserDto>>("/users");

        list.Should().NotBeNull();
        list!.Should().HaveCount(2);
        list.Select(u => u.Email).Should().BeEquivalentTo(new[] { "a@example.com", "b@example.com" });
    }
}
