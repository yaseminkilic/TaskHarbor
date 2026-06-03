using FluentValidation;
using TaskTracker.Application.Abstractions;
using TaskTracker.Application.Abstractions.Repositories;
using TaskTracker.Application.Common.Exceptions;
using TaskTracker.Application.Tasks.Dtos;
using TaskTracker.Domain.Entities;
using DomainTaskStatus = TaskTracker.Domain.Entities.TaskStatus;
using ValidationException = TaskTracker.Application.Common.Exceptions.ValidationException;

namespace TaskTracker.Application.Tasks.Services;

public class TaskService : ITaskService
{
    private readonly ITaskRepository _tasks;
    private readonly IProjectRepository _projects;
    private readonly IUnitOfWork _uow;
    private readonly IValidator<CreateTaskRequest> _createValidator;
    private readonly IValidator<UpdateTaskRequest> _updateValidator;

    public TaskService(
        ITaskRepository tasks,
        IProjectRepository projects,
        IUnitOfWork uow,
        IValidator<CreateTaskRequest> createValidator,
        IValidator<UpdateTaskRequest> updateValidator)
    {
        _tasks = tasks;
        _projects = projects;
        _uow = uow;
        _createValidator = createValidator;
        _updateValidator = updateValidator;
    }

    public async Task<TaskDto> CreateAsync(CreateTaskRequest request, CancellationToken ct = default)
    {
        var result = await _createValidator.ValidateAsync(request, ct);
        if (!result.IsValid)
            throw new ValidationException(result.Errors);

        var project = await _projects.GetByIdAsync(request.ProjectId, ct)
            ?? throw new NotFoundException(nameof(Project), request.ProjectId);

        var task = project.AddTask(request.Title, request.Description);
        await _tasks.AddAsync(task, ct);
        await _uow.SaveChangesAsync(ct);

        return TaskDto.FromEntity(task);
    }

    public async Task<TaskDto> GetByIdAsync(Guid id, CancellationToken ct = default)
    {
        var task = await _tasks.GetByIdWithTagsAsync(id, ct)
            ?? throw new NotFoundException(nameof(TaskItem), id);
        return TaskDto.FromEntity(task);
    }

    public async Task<IReadOnlyList<TaskDto>> ListByProjectAsync(
        Guid projectId,
        string? status,
        CancellationToken ct = default)
    {
        DomainTaskStatus? statusFilter = null;
        if (!string.IsNullOrWhiteSpace(status))
        {
            if (!Enum.TryParse<DomainTaskStatus>(status, ignoreCase: true, out var parsed))
                throw new ValidationException(nameof(status), $"Unknown status value '{status}'.");
            statusFilter = parsed;
        }

        var tasks = await _tasks.ListByProjectAsync(projectId, statusFilter, ct);
        return tasks.Select(TaskDto.FromEntity).ToList();
    }

    public async Task<TaskDto> UpdateAsync(Guid id, UpdateTaskRequest request, CancellationToken ct = default)
    {
        var result = await _updateValidator.ValidateAsync(request, ct);
        if (!result.IsValid)
            throw new ValidationException(result.Errors);

        var task = await _tasks.GetByIdWithTagsAsync(id, ct)
            ?? throw new NotFoundException(nameof(TaskItem), id);

        task.UpdateDetails(request.Title, request.Description);
        await _uow.SaveChangesAsync(ct);

        return TaskDto.FromEntity(task);
    }

    public async Task<TaskDto> StartAsync(Guid id, CancellationToken ct = default)
    {
        var task = await _tasks.GetByIdWithTagsAsync(id, ct)
            ?? throw new NotFoundException(nameof(TaskItem), id);

        task.Start();
        await _uow.SaveChangesAsync(ct);

        return TaskDto.FromEntity(task);
    }

    public async Task<TaskDto> CompleteAsync(Guid id, CancellationToken ct = default)
    {
        var task = await _tasks.GetByIdWithTagsAsync(id, ct)
            ?? throw new NotFoundException(nameof(TaskItem), id);

        task.Complete();
        await _uow.SaveChangesAsync(ct);

        return TaskDto.FromEntity(task);
    }

    public async Task DeleteAsync(Guid id, CancellationToken ct = default)
    {
        var task = await _tasks.GetByIdAsync(id, ct)
            ?? throw new NotFoundException(nameof(TaskItem), id);

        _tasks.Remove(task);
        await _uow.SaveChangesAsync(ct);
    }

    public async Task<TaskDto> AddTagAsync(Guid id, string tagName, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(tagName))
            throw new ValidationException(nameof(tagName), "Tag name is required.");

        var task = await _tasks.GetByIdWithTagsAsync(id, ct)
            ?? throw new NotFoundException(nameof(TaskItem), id);

        var normalized = tagName.Trim().ToLowerInvariant();
        var tag = await _tasks.GetTagByNameAsync(normalized, ct);
        if (tag is null)
        {
            tag = new Tag(normalized);
            await _tasks.AddTagAsync(tag, ct);
        }

        task.AddTag(tag);
        await _uow.SaveChangesAsync(ct);

        return TaskDto.FromEntity(task);
    }

    public async Task<TaskDto> RemoveTagAsync(Guid id, string tagName, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(tagName))
            throw new ValidationException(nameof(tagName), "Tag name is required.");

        var task = await _tasks.GetByIdWithTagsAsync(id, ct)
            ?? throw new NotFoundException(nameof(TaskItem), id);

        var normalized = tagName.Trim().ToLowerInvariant();
        var tag = task.Tags.FirstOrDefault(t => t.Name == normalized);
        if (tag is not null)
        {
            task.RemoveTag(tag);
            await _uow.SaveChangesAsync(ct);
        }

        return TaskDto.FromEntity(task);
    }
}
