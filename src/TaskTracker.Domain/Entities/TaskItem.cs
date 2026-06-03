using TaskTracker.Domain.Common;

namespace TaskTracker.Domain.Entities;

public class TaskItem
{
    public Guid Id { get; private set; }
    public string Title { get; private set; } = default!;
    public string? Description { get; private set; }
    public TaskStatus Status { get; private set; }
    public DateTime CreatedAtUtc { get; private set; }
    public DateTime? CompletedAtUtc { get; private set; }

    public Guid ProjectId { get; private set; }
    public Project Project { get; private set; } = default!;

    public ICollection<Tag> Tags { get; private set; } = new List<Tag>();

    private TaskItem() { }

    internal TaskItem(Project project, string title, string? description)
    {
        if (project is null)
            throw new DomainException("Project is required.");
        if (string.IsNullOrWhiteSpace(title))
            throw new DomainException("Title is required.");

        Id = Guid.NewGuid();
        Title = title.Trim();
        Description = string.IsNullOrWhiteSpace(description) ? null : description.Trim();
        Status = TaskStatus.Open;
        CreatedAtUtc = DateTime.UtcNow;

        ProjectId = project.Id;
        Project = project;
    }

    public void Start()
    {
        if (Status == TaskStatus.Done)
            throw new DomainException("A completed task cannot be restarted.");
        Status = TaskStatus.InProgress;
    }

    public void Complete()
    {
        if (Status == TaskStatus.Done)
            return;
        Status = TaskStatus.Done;
        CompletedAtUtc = DateTime.UtcNow;
    }

    public void UpdateDetails(string title, string? description)
    {
        if (Status == TaskStatus.Done)
            throw new DomainException("A completed task cannot be edited.");
        if (string.IsNullOrWhiteSpace(title))
            throw new DomainException("Title is required.");

        Title = title.Trim();
        Description = string.IsNullOrWhiteSpace(description) ? null : description.Trim();
    }

    public void AddTag(Tag tag)
    {
        if (tag is null)
            throw new DomainException("Tag is required.");
        if (Tags.Any(t => t.Id == tag.Id))
            return;
        Tags.Add(tag);
    }

    public void RemoveTag(Tag tag)
    {
        var existing = Tags.FirstOrDefault(t => t.Id == tag.Id);
        if (existing is not null)
            Tags.Remove(existing);
    }
}
