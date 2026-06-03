using FluentAssertions;
using TaskTracker.Application.Common.Exceptions;
using TaskTracker.Application.Tasks.Dtos;
using TaskTracker.Application.Tasks.Services;
using TaskTracker.Application.Tasks.Validators;
using TaskTracker.Domain.Common;
using TaskTracker.Domain.Entities;
using TaskTracker.UnitTests.Application.Fixtures;
using TaskTracker.UnitTests.Common.Builders;

namespace TaskTracker.UnitTests.Application.Tasks;

public class TaskServiceTests : IDisposable
{
    private readonly SqliteInMemoryFixture _fx = new();
    private readonly TaskService _sut;

    public TaskServiceTests()
    {
        _sut = new TaskService(
            _fx.Tasks,
            _fx.Projects,
            _fx.UnitOfWork,
            new CreateTaskRequestValidator(),
            new UpdateTaskRequestValidator());
    }

    public void Dispose() => _fx.Dispose();

    private async Task<Project> SeedProjectAsync()
    {
        var owner = new UserBuilder().Build();
        var project = new ProjectBuilder().WithOwner(owner).Build();
        await _fx.Users.AddAsync(owner);
        await _fx.Projects.AddAsync(project);
        await _fx.UnitOfWork.SaveChangesAsync();
        return project;
    }

    // ---------- CreateAsync ----------

    [Fact]
    public async Task CreateAsync_ProjectNotFound_Throws()
    {
        var act = () => _sut.CreateAsync(new CreateTaskRequest(Guid.NewGuid(), "title", null));

        var ex = await act.Should().ThrowAsync<NotFoundException>();
        ex.Which.Entity.Should().Be(nameof(Project));
    }

    // Regression for Adım 7 bug #1: ctor sets Id => EF used to mark as Modified,
    // so the row never reached the DB. Service now calls Tasks.AddAsync explicitly.
    [Fact]
    public async Task CreateAsync_PersistsTask_AndDtoHasOpenStatus()
    {
        var project = await SeedProjectAsync();

        var dto = await _sut.CreateAsync(new CreateTaskRequest(project.Id, "Write tests", "for the app"));

        dto.Id.Should().NotBe(Guid.Empty);
        dto.ProjectId.Should().Be(project.Id);
        dto.Status.Should().Be("Open");
        dto.Tags.Should().BeEmpty();

        var persisted = await _fx.Tasks.GetByIdAsync(dto.Id);
        persisted.Should().NotBeNull();
        persisted!.Title.Should().Be("Write tests");
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public async Task CreateAsync_EmptyTitle_ThrowsValidation(string title)
    {
        var project = await SeedProjectAsync();

        var act = () => _sut.CreateAsync(new CreateTaskRequest(project.Id, title, null));

        await act.Should().ThrowAsync<ValidationException>();
    }

    // ---------- StartAsync ----------

    [Fact]
    public async Task StartAsync_MovesOpenToInProgress()
    {
        var project = await SeedProjectAsync();
        var dto = await _sut.CreateAsync(new CreateTaskRequest(project.Id, "t", null));

        var started = await _sut.StartAsync(dto.Id);

        started.Status.Should().Be("InProgress");
    }

    [Fact]
    public async Task StartAsync_AfterComplete_ThrowsDomain()
    {
        var project = await SeedProjectAsync();
        var dto = await _sut.CreateAsync(new CreateTaskRequest(project.Id, "t", null));
        await _sut.CompleteAsync(dto.Id);

        var act = () => _sut.StartAsync(dto.Id);

        await act.Should().ThrowAsync<DomainException>().WithMessage("*completed task*");
    }

    // ---------- CompleteAsync ----------

    // Regression for Adım 7 bug #2: Start/Complete used to call GetByIdAsync (no Include),
    // so tags were missing from the response DTO. Service now uses GetByIdWithTagsAsync.
    [Fact]
    public async Task CompleteAsync_PreservesTags_InDto()
    {
        var project = await SeedProjectAsync();
        var dto = await _sut.CreateAsync(new CreateTaskRequest(project.Id, "t", null));
        await _sut.AddTagAsync(dto.Id, "urgent");

        var completed = await _sut.CompleteAsync(dto.Id);

        completed.Status.Should().Be("Done");
        completed.Tags.Should().ContainSingle().Which.Should().Be("urgent");
        completed.CompletedAtUtc.Should().NotBeNull();
    }

    // ---------- ListByProjectAsync ----------

    [Fact]
    public async Task ListByProjectAsync_NoFilter_ReturnsAll()
    {
        var project = await SeedProjectAsync();
        await _sut.CreateAsync(new CreateTaskRequest(project.Id, "a", null));
        await _sut.CreateAsync(new CreateTaskRequest(project.Id, "b", null));

        var list = await _sut.ListByProjectAsync(project.Id, status: null);

        list.Should().HaveCount(2);
    }

    [Fact]
    public async Task ListByProjectAsync_FiltersByStatus_CaseInsensitive()
    {
        var project = await SeedProjectAsync();
        var a = await _sut.CreateAsync(new CreateTaskRequest(project.Id, "a", null));
        var b = await _sut.CreateAsync(new CreateTaskRequest(project.Id, "b", null));
        await _sut.StartAsync(a.Id);
        await _sut.CompleteAsync(b.Id);

        var inProgress = await _sut.ListByProjectAsync(project.Id, status: "inprogress");

        inProgress.Should().ContainSingle().Which.Id.Should().Be(a.Id);
    }

    [Fact]
    public async Task ListByProjectAsync_InvalidStatusString_ThrowsValidation()
    {
        var project = await SeedProjectAsync();

        var act = () => _sut.ListByProjectAsync(project.Id, status: "Bogus");

        var ex = await act.Should().ThrowAsync<ValidationException>();
        ex.Which.Errors.Should().ContainKey("status");
    }

    // ---------- UpdateAsync ----------

    [Fact]
    public async Task UpdateAsync_UpdatesTitleAndDescription()
    {
        var project = await SeedProjectAsync();
        var dto = await _sut.CreateAsync(new CreateTaskRequest(project.Id, "old", "old desc"));

        var updated = await _sut.UpdateAsync(dto.Id, new UpdateTaskRequest("new title", "new desc"));

        updated.Title.Should().Be("new title");
        updated.Description.Should().Be("new desc");
    }

    [Fact]
    public async Task UpdateAsync_AfterDone_ThrowsDomain()
    {
        var project = await SeedProjectAsync();
        var dto = await _sut.CreateAsync(new CreateTaskRequest(project.Id, "t", null));
        await _sut.CompleteAsync(dto.Id);

        var act = () => _sut.UpdateAsync(dto.Id, new UpdateTaskRequest("new", null));

        await act.Should().ThrowAsync<DomainException>().WithMessage("*completed task*");
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public async Task UpdateAsync_EmptyTitle_ThrowsValidation(string title)
    {
        var project = await SeedProjectAsync();
        var dto = await _sut.CreateAsync(new CreateTaskRequest(project.Id, "t", null));

        var act = () => _sut.UpdateAsync(dto.Id, new UpdateTaskRequest(title, null));

        await act.Should().ThrowAsync<ValidationException>();
    }

    [Fact]
    public async Task UpdateAsync_NotFound_Throws()
    {
        var act = () => _sut.UpdateAsync(Guid.NewGuid(), new UpdateTaskRequest("t", null));

        await act.Should().ThrowAsync<NotFoundException>();
    }

    // ---------- DeleteAsync ----------

    [Fact]
    public async Task DeleteAsync_NotFound_Throws()
    {
        var act = () => _sut.DeleteAsync(Guid.NewGuid());

        await act.Should().ThrowAsync<NotFoundException>();
    }

    [Fact]
    public async Task DeleteAsync_RemovesEntity()
    {
        var project = await SeedProjectAsync();
        var dto = await _sut.CreateAsync(new CreateTaskRequest(project.Id, "t", null));

        await _sut.DeleteAsync(dto.Id);

        var found = await _fx.Tasks.GetByIdAsync(dto.Id);
        found.Should().BeNull();
    }

    // ---------- AddTagAsync ----------

    [Fact]
    public async Task AddTagAsync_CreatesTagIfMissing_AndNormalizes()
    {
        var project = await SeedProjectAsync();
        var dto = await _sut.CreateAsync(new CreateTaskRequest(project.Id, "t", null));

        var result = await _sut.AddTagAsync(dto.Id, "  Urgent  ");

        result.Tags.Should().ContainSingle().Which.Should().Be("urgent");

        var tagRow = await _fx.Tasks.GetTagByNameAsync("urgent");
        tagRow.Should().NotBeNull();
    }

    [Fact]
    public async Task AddTagAsync_ReusesExistingTag_CaseInsensitive()
    {
        var project = await SeedProjectAsync();
        var a = await _sut.CreateAsync(new CreateTaskRequest(project.Id, "a", null));
        var b = await _sut.CreateAsync(new CreateTaskRequest(project.Id, "b", null));
        await _sut.AddTagAsync(a.Id, "Urgent");

        await _sut.AddTagAsync(b.Id, "URGENT");

        var allTags = _fx.Db.Tags.ToList();
        allTags.Should().ContainSingle().Which.Name.Should().Be("urgent");
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public async Task AddTagAsync_EmptyName_ThrowsValidation(string tagName)
    {
        var project = await SeedProjectAsync();
        var dto = await _sut.CreateAsync(new CreateTaskRequest(project.Id, "t", null));

        var act = () => _sut.AddTagAsync(dto.Id, tagName);

        await act.Should().ThrowAsync<ValidationException>();
    }

    [Fact]
    public async Task AddTagAsync_TaskNotFound_Throws()
    {
        var act = () => _sut.AddTagAsync(Guid.NewGuid(), "urgent");

        await act.Should().ThrowAsync<NotFoundException>();
    }

    // ---------- RemoveTagAsync ----------

    [Fact]
    public async Task RemoveTagAsync_RemovesExistingTag_FromTask()
    {
        var project = await SeedProjectAsync();
        var dto = await _sut.CreateAsync(new CreateTaskRequest(project.Id, "t", null));
        await _sut.AddTagAsync(dto.Id, "urgent");

        var result = await _sut.RemoveTagAsync(dto.Id, "URGENT");

        result.Tags.Should().BeEmpty();
    }

    [Fact]
    public async Task RemoveTagAsync_NonExistingTag_NoOp()
    {
        var project = await SeedProjectAsync();
        var dto = await _sut.CreateAsync(new CreateTaskRequest(project.Id, "t", null));
        await _sut.AddTagAsync(dto.Id, "urgent");

        var result = await _sut.RemoveTagAsync(dto.Id, "nope");

        result.Tags.Should().ContainSingle().Which.Should().Be("urgent");
    }
}
