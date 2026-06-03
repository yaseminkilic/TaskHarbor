using TaskTracker.Domain.Entities;

namespace TaskTracker.Application.Abstractions.Repositories;

public interface IProjectRepository
{
    Task<Project?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<Project?> GetByIdWithTasksAsync(Guid id, CancellationToken ct = default);
    Task<IReadOnlyList<Project>> ListByOwnerAsync(Guid ownerId, CancellationToken ct = default);
    Task AddAsync(Project project, CancellationToken ct = default);
}
