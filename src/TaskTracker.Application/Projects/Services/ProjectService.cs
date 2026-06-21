using FluentValidation;
using TaskTracker.Application.Abstractions;
using TaskTracker.Application.Abstractions.Repositories;
using TaskTracker.Application.Common.Exceptions;
using TaskTracker.Application.Projects.Dtos;
using TaskTracker.Domain.Entities;
using ValidationException = TaskTracker.Application.Common.Exceptions.ValidationException;

namespace TaskTracker.Application.Projects.Services;

public class ProjectService : IProjectService
{
    private readonly IProjectRepository _projects;
    private readonly IUserRepository _users;
    private readonly IUnitOfWork _uow;
    private readonly IValidator<CreateProjectRequest> _createValidator;

    public ProjectService(
        IProjectRepository projects,
        IUserRepository users,
        IUnitOfWork uow,
        IValidator<CreateProjectRequest> createValidator)
    {
        _projects = projects;
        _users = users;
        _uow = uow;
        _createValidator = createValidator;
    }

    public async Task<ProjectDto> CreateAsync(CreateProjectRequest request, CancellationToken ct = default)
    {
        var result = await _createValidator.ValidateAsync(request, ct);
        if (!result.IsValid)
            throw new ValidationException(result.Errors);

        var owner = await _users.GetByIdAsync(request.OwnerId, ct)
            ?? throw new NotFoundException(nameof(User), request.OwnerId);

        var project = new Project(request.Name, owner);
        await _projects.AddAsync(project, ct);
        await _uow.SaveChangesAsync(ct);

        return ProjectDto.FromEntity(project);
    }

    public async Task<ProjectDto> GetByIdAsync(Guid id, CancellationToken ct = default)
    {
        return await _projects.GetDtoByIdAsync(id, ct)
            ?? throw new NotFoundException(nameof(Project), id);
    }

    public Task<IReadOnlyList<ProjectDto>> ListByOwnerAsync(Guid ownerId, CancellationToken ct = default)
        => _projects.ListByOwnerAsync(ownerId, ct);
}
