using TaskTracker.Application.Projects.Dtos;
using TaskTracker.Domain.Entities;

namespace TaskTracker.Application.Abstractions.Repositories;

public interface IProjectRepository
{
    Task<Project?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<ProjectDto?> GetDtoByIdAsync(Guid id, CancellationToken ct = default);
    Task<IReadOnlyList<ProjectDto>> ListByOwnerAsync(Guid ownerId, CancellationToken ct = default);
    Task AddAsync(Project project, CancellationToken ct = default);
}
