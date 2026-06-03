using TaskTracker.Domain.Entities;

namespace TaskTracker.UnitTests.Common.Builders;

public class ProjectBuilder
{
    private string _name = "Default Project";
    private User? _owner;

    public ProjectBuilder WithName(string name) { _name = name; return this; }
    public ProjectBuilder WithOwner(User owner) { _owner = owner; return this; }

    public Project Build() => new(_name, _owner ?? new UserBuilder().Build());
}
