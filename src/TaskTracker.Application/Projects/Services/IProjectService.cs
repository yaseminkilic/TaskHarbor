using TaskTracker.Application.Projects.Dtos;

namespace TaskTracker.Application.Projects.Services;

public interface IProjectService
{
    Task<ProjectDto> CreateAsync(CreateProjectRequest request, CancellationToken ct = default);
    Task<ProjectDto> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<IReadOnlyList<ProjectDto>> ListByOwnerAsync(Guid ownerId, CancellationToken ct = default);
}
