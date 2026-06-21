using TaskTracker.Domain.Entities;

namespace TaskTracker.Application.Abstractions.Repositories;

public interface ITaskRepository
{
    Task<TaskItem?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<TaskItem?> GetByIdWithTagsAsync(Guid id, CancellationToken ct = default);
    Task<IReadOnlyList<TaskItem>> ListByProjectAsync(
        Guid projectId,
        Domain.Entities.TaskStatus? statusFilter,
        CancellationToken ct = default);
    Task AddAsync(TaskItem task, CancellationToken ct = default);
    Task<Tag?> GetTagByNameAsync(string name, CancellationToken ct = default);
    Task<Tag> GetOrCreateTagAsync(string name, CancellationToken ct = default);
    void Remove(TaskItem task);
}
