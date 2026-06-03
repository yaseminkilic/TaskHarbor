using TaskTracker.Application.Tasks.Dtos;

namespace TaskTracker.Application.Tasks.Services;

public interface ITaskService
{
    Task<TaskDto> CreateAsync(CreateTaskRequest request, CancellationToken ct = default);
    Task<TaskDto> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<IReadOnlyList<TaskDto>> ListByProjectAsync(
        Guid projectId,
        string? status,
        CancellationToken ct = default);

    Task<TaskDto> UpdateAsync(Guid id, UpdateTaskRequest request, CancellationToken ct = default);
    Task<TaskDto> StartAsync(Guid id, CancellationToken ct = default);
    Task<TaskDto> CompleteAsync(Guid id, CancellationToken ct = default);
    Task DeleteAsync(Guid id, CancellationToken ct = default);

    Task<TaskDto> AddTagAsync(Guid id, string tagName, CancellationToken ct = default);
    Task<TaskDto> RemoveTagAsync(Guid id, string tagName, CancellationToken ct = default);
}
