using FluentAssertions;
using TaskTracker.Domain.Common;
using TaskTracker.Domain.Entities;
using TaskTracker.UnitTests.Common.Builders;

namespace TaskTracker.UnitTests.Domain;

public class ProjectTests
{
    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Constructor_WithEmptyName_Throws(string name)
    {
        var owner = new UserBuilder().Build();

        var act = () => new Project(name, owner);

        act.Should().Throw<DomainException>().WithMessage("*name*");
    }

    [Fact]
    public void Constructor_WithNullOwner_Throws()
    {
        var act = () => new Project("Some project", null!);

        act.Should().Throw<DomainException>().WithMessage("*Owner*");
    }

    [Fact]
    public void Constructor_TrimsName_AndCapturesOwner()
    {
        var owner = new UserBuilder().Build();

        var project = new Project("  Apollo  ", owner);

        project.Name.Should().Be("Apollo");
        project.OwnerId.Should().Be(owner.Id);
        project.Owner.Should().BeSameAs(owner);
        project.Id.Should().NotBe(Guid.Empty);
    }

    [Fact]
    public void AddTask_AddsToCollection_AndReturnsTask()
    {
        var project = new ProjectBuilder().Build();

        var task = project.AddTask("Write tests", "for the domain layer");

        task.Should().NotBeNull();
        task.Title.Should().Be("Write tests");
        task.Description.Should().Be("for the domain layer");
        task.ProjectId.Should().Be(project.Id);
        project.Tasks.Should().ContainSingle().Which.Should().BeSameAs(task);
    }

    [Fact]
    public void AddTask_WithoutDescription_LeavesDescriptionNull()
    {
        var project = new ProjectBuilder().Build();

        var task = project.AddTask("Quick task");

        task.Description.Should().BeNull();
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Rename_WithEmpty_Throws(string newName)
    {
        var project = new ProjectBuilder().Build();

        var act = () => project.Rename(newName);

        act.Should().Throw<DomainException>().WithMessage("*name*");
    }

    [Fact]
    public void Rename_TrimsAndUpdates()
    {
        var project = new ProjectBuilder().Build();

        project.Rename("  New Name  ");

        project.Name.Should().Be("New Name");
    }
}
