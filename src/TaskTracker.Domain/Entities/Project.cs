using TaskTracker.Domain.Common;

namespace TaskTracker.Domain.Entities;

public class Project
{
    public Guid Id { get; private set; }
    public string Name { get; private set; } = default!;
    public Guid OwnerId { get; private set; }
    public User Owner { get; private set; } = default!;
    public DateTime CreatedAtUtc { get; private set; }

    public ICollection<TaskItem> Tasks { get; private set; } = new List<TaskItem>();

    private Project() { }

    public Project(string name, User owner)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new DomainException("Project name is required.");
        if (owner is null)
            throw new DomainException("Owner is required.");

        Id = Guid.NewGuid();
        Name = name.Trim();
        OwnerId = owner.Id;
        Owner = owner;
        CreatedAtUtc = DateTime.UtcNow;
    }

    public void Rename(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new DomainException("Project name is required.");
        Name = name.Trim();
    }

    public TaskItem AddTask(string title, string? description = null)
    {
        var task = new TaskItem(this, title, description);
        Tasks.Add(task);
        return task;
    }
}
