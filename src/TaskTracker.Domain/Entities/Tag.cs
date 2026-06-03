using TaskTracker.Domain.Common;

namespace TaskTracker.Domain.Entities;

public class Tag
{
    public Guid Id { get; private set; }
    public string Name { get; private set; } = default!;

    public ICollection<TaskItem> Tasks { get; private set; } = new List<TaskItem>();

    private Tag() { }

    public Tag(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new DomainException("Tag name is required.");

        Id = Guid.NewGuid();
        Name = name.Trim().ToLowerInvariant();
    }
}
