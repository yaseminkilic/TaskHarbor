using Microsoft.EntityFrameworkCore;
using TaskTracker.Application.Abstractions.Repositories;
using TaskTracker.Application.Projects.Dtos;
using TaskTracker.Domain.Entities;

namespace TaskTracker.Infrastructure.Persistence.Repositories;

public class ProjectRepository : IProjectRepository
{
    private readonly AppDbContext _db;

    public ProjectRepository(AppDbContext db) => _db = db;

    public Task<Project?> GetByIdAsync(Guid id, CancellationToken ct = default) =>
        _db.Projects.FirstOrDefaultAsync(p => p.Id == id, ct);

    public Task<ProjectDto?> GetDtoByIdAsync(Guid id, CancellationToken ct = default) =>
        _db.Projects
            .AsNoTracking()
            .Where(p => p.Id == id)
            .Select(p => new ProjectDto(p.Id, p.Name, p.OwnerId, p.CreatedAtUtc, p.Tasks.Count))
            .FirstOrDefaultAsync(ct);

    public async Task<IReadOnlyList<ProjectDto>> ListByOwnerAsync(Guid ownerId, CancellationToken ct = default) =>
        await _db.Projects
            .AsNoTracking()
            .Where(p => p.OwnerId == ownerId)
            .OrderBy(p => p.CreatedAtUtc)
            .Select(p => new ProjectDto(p.Id, p.Name, p.OwnerId, p.CreatedAtUtc, p.Tasks.Count))
            .ToListAsync(ct);

    public async Task AddAsync(Project project, CancellationToken ct = default)
    {
        await _db.Projects.AddAsync(project, ct);
    }
}
