using FluentAssertions;
using TaskTracker.Application.Common.Exceptions;
using TaskTracker.Application.Projects.Dtos;
using TaskTracker.Application.Projects.Services;
using TaskTracker.Application.Projects.Validators;
using TaskTracker.Domain.Entities;
using TaskTracker.UnitTests.Application.Fixtures;
using TaskTracker.UnitTests.Common.Builders;

namespace TaskTracker.UnitTests.Application.Projects;

public class ProjectServiceTests : IDisposable
{
    private readonly SqliteInMemoryFixture _fx = new();
    private readonly ProjectService _sut;

    public ProjectServiceTests()
    {
        _sut = new ProjectService(_fx.Projects, _fx.Users, _fx.UnitOfWork, new CreateProjectRequestValidator());
    }

    public void Dispose() => _fx.Dispose();

    private async Task<User> SeedUserAsync()
    {
        var user = new UserBuilder().Build();
        await _fx.Users.AddAsync(user);
        await _fx.UnitOfWork.SaveChangesAsync();
        return user;
    }

    [Fact]
    public async Task CreateAsync_PersistsProject_AndReturnsDto()
    {
        var owner = await SeedUserAsync();

        var dto = await _sut.CreateAsync(new CreateProjectRequest(owner.Id, "Apollo"));

        dto.Name.Should().Be("Apollo");
        dto.OwnerId.Should().Be(owner.Id);
        dto.TaskCount.Should().Be(0);

        var persisted = await _fx.Projects.GetByIdAsync(dto.Id);
        persisted.Should().NotBeNull();
        persisted!.Name.Should().Be("Apollo");
    }

    [Fact]
    public async Task CreateAsync_OwnerNotFound_ThrowsNotFound()
    {
        var act = () => _sut.CreateAsync(new CreateProjectRequest(Guid.NewGuid(), "Apollo"));

        var ex = await act.Should().ThrowAsync<NotFoundException>();
        ex.Which.Entity.Should().Be(nameof(User));
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public async Task CreateAsync_InvalidName_ThrowsValidation(string name)
    {
        var owner = await SeedUserAsync();

        var act = () => _sut.CreateAsync(new CreateProjectRequest(owner.Id, name));

        await act.Should().ThrowAsync<ValidationException>();
    }

    [Fact]
    public async Task CreateAsync_EmptyOwnerId_ThrowsValidation()
    {
        var act = () => _sut.CreateAsync(new CreateProjectRequest(Guid.Empty, "Apollo"));

        await act.Should().ThrowAsync<ValidationException>();
    }

    [Fact]
    public async Task GetByIdAsync_NotFound_Throws()
    {
        var act = () => _sut.GetByIdAsync(Guid.NewGuid());

        var ex = await act.Should().ThrowAsync<NotFoundException>();
        ex.Which.Entity.Should().Be(nameof(Project));
    }

    [Fact]
    public async Task GetByIdAsync_IncludesTaskCount()
    {
        var owner = await SeedUserAsync();
        var project = new ProjectBuilder().WithOwner(owner).Build();
        project.AddTask("t1");
        project.AddTask("t2");
        await _fx.Projects.AddAsync(project);
        await _fx.UnitOfWork.SaveChangesAsync();

        var dto = await _sut.GetByIdAsync(project.Id);

        dto.TaskCount.Should().Be(2);
    }

    [Fact]
    public async Task ListByOwnerAsync_ReturnsOnlyOwnerProjects()
    {
        var alice = await SeedUserAsync();
        var bob = new UserBuilder().WithEmail("bob@example.com").WithDisplayName("Bob").Build();
        await _fx.Users.AddAsync(bob);
        await _fx.UnitOfWork.SaveChangesAsync();

        await _sut.CreateAsync(new CreateProjectRequest(alice.Id, "Alice's Project"));
        await _sut.CreateAsync(new CreateProjectRequest(bob.Id, "Bob's Project"));

        var list = await _sut.ListByOwnerAsync(alice.Id);

        list.Should().ContainSingle().Which.Name.Should().Be("Alice's Project");
    }
}
