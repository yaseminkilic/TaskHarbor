using TaskTracker.Domain.Entities;

namespace TaskTracker.UnitTests.Common.Builders;

public class TaskItemBuilder
{
    private string _title = "Default Title";
    private string? _description;
    private Project? _project;

    public TaskItemBuilder WithTitle(string title) { _title = title; return this; }
    public TaskItemBuilder WithDescription(string? description) { _description = description; return this; }
    public TaskItemBuilder InProject(Project project) { _project = project; return this; }

    // TaskItem's constructor is internal — instances must be created via Project.AddTask.
    public TaskItem Build()
    {
        var project = _project ?? new ProjectBuilder().Build();
        return project.AddTask(_title, _description);
    }
}
