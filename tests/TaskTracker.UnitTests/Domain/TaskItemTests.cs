using FluentAssertions;
using TaskTracker.Domain.Common;
using TaskTracker.Domain.Entities;
using TaskTracker.UnitTests.Common.Builders;
using TaskStatus = TaskTracker.Domain.Entities.TaskStatus;

namespace TaskTracker.UnitTests.Domain;

public class TaskItemTests
{
    [Fact]
    public void Created_HasOpenStatus_AndNoCompletedTimestamp()
    {
        var task = new TaskItemBuilder().Build();

        task.Status.Should().Be(TaskStatus.Open);
        task.CompletedAtUtc.Should().BeNull();
        task.Id.Should().NotBe(Guid.Empty);
    }

    [Fact]
    public void Created_TrimsTitleAndDescription()
    {
        var project = new ProjectBuilder().Build();

        var task = project.AddTask("  Write tests  ", "  do it properly  ");

        task.Title.Should().Be("Write tests");
        task.Description.Should().Be("do it properly");
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Created_WithEmptyTitle_Throws(string title)
    {
        var project = new ProjectBuilder().Build();

        var act = () => project.AddTask(title);

        act.Should().Throw<DomainException>().WithMessage("*Title*");
    }

    [Fact]
    public void Start_MovesOpenToInProgress()
    {
        var task = new TaskItemBuilder().Build();

        task.Start();

        task.Status.Should().Be(TaskStatus.InProgress);
    }

    [Fact]
    public void Start_OnInProgress_IsIdempotent()
    {
        var task = new TaskItemBuilder().Build();
        task.Start();

        var act = () => task.Start();

        act.Should().NotThrow();
        task.Status.Should().Be(TaskStatus.InProgress);
    }

    [Fact]
    public void Start_AfterDone_Throws()
    {
        var task = new TaskItemBuilder().Build();
        task.Complete();

        var act = () => task.Start();

        act.Should().Throw<DomainException>().WithMessage("*completed task*");
    }

    [Fact]
    public void Complete_SetsStatusAndTimestamp()
    {
        var task = new TaskItemBuilder().Build();
        var before = DateTime.UtcNow;

        task.Complete();
        var after = DateTime.UtcNow;

        task.Status.Should().Be(TaskStatus.Done);
        task.CompletedAtUtc.Should().NotBeNull();
        task.CompletedAtUtc!.Value.Should().BeOnOrAfter(before).And.BeOnOrBefore(after);
    }

    [Fact]
    public void Complete_Idempotent_DoesNotChangeTimestamp()
    {
        var task = new TaskItemBuilder().Build();
        task.Complete();
        var firstTimestamp = task.CompletedAtUtc;

        task.Complete();

        task.Status.Should().Be(TaskStatus.Done);
        task.CompletedAtUtc.Should().Be(firstTimestamp);
    }

    [Fact]
    public void UpdateDetails_AfterDone_Throws()
    {
        var task = new TaskItemBuilder().Build();
        task.Complete();

        var act = () => task.UpdateDetails("new title", null);

        act.Should().Throw<DomainException>().WithMessage("*completed task*");
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void UpdateDetails_WithEmptyTitle_Throws(string title)
    {
        var task = new TaskItemBuilder().Build();

        var act = () => task.UpdateDetails(title, null);

        act.Should().Throw<DomainException>().WithMessage("*Title*");
    }

    [Fact]
    public void UpdateDetails_TrimsTitleAndDescription()
    {
        var task = new TaskItemBuilder().Build();

        task.UpdateDetails("  New Title  ", "  New Desc  ");

        task.Title.Should().Be("New Title");
        task.Description.Should().Be("New Desc");
    }

    [Fact]
    public void UpdateDetails_WithEmptyDescription_NormalizesToNull()
    {
        var task = new TaskItemBuilder().WithDescription("initial").Build();

        task.UpdateDetails("Title", "   ");

        task.Description.Should().BeNull();
    }

    [Fact]
    public void AddTag_AddsToCollection()
    {
        var task = new TaskItemBuilder().Build();
        var tag = new Tag("urgent");

        task.AddTag(tag);

        task.Tags.Should().ContainSingle().Which.Should().BeSameAs(tag);
    }

    [Fact]
    public void AddTag_Null_Throws()
    {
        var task = new TaskItemBuilder().Build();

        var act = () => task.AddTag(null!);

        act.Should().Throw<DomainException>().WithMessage("*Tag*");
    }

    [Fact]
    public void AddTag_DuplicateById_NoOp()
    {
        var task = new TaskItemBuilder().Build();
        var tag = new Tag("urgent");
        task.AddTag(tag);

        task.AddTag(tag);

        task.Tags.Should().ContainSingle();
    }

    [Fact]
    public void RemoveTag_Existing_Removes()
    {
        var task = new TaskItemBuilder().Build();
        var tag = new Tag("urgent");
        task.AddTag(tag);

        task.RemoveTag(tag);

        task.Tags.Should().BeEmpty();
    }

    [Fact]
    public void RemoveTag_NonExisting_NoOp()
    {
        var task = new TaskItemBuilder().Build();
        var existing = new Tag("urgent");
        task.AddTag(existing);
        var other = new Tag("other");

        var act = () => task.RemoveTag(other);

        act.Should().NotThrow();
        task.Tags.Should().ContainSingle().Which.Should().BeSameAs(existing);
    }
}
