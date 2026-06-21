using Microsoft.EntityFrameworkCore;
using TaskTracker.Application.Abstractions.Repositories;
using TaskTracker.Domain.Entities;
using DomainTaskStatus = TaskTracker.Domain.Entities.TaskStatus;

namespace TaskTracker.Infrastructure.Persistence.Repositories;

public class TaskRepository : ITaskRepository
{
    private readonly AppDbContext _db;

    public TaskRepository(AppDbContext db) => _db = db;

    public Task<TaskItem?> GetByIdAsync(Guid id, CancellationToken ct = default) =>
        _db.Tasks.FirstOrDefaultAsync(t => t.Id == id, ct);

    public Task<TaskItem?> GetByIdWithTagsAsync(Guid id, CancellationToken ct = default) =>
        _db.Tasks
            .Include(t => t.Tags)
            .FirstOrDefaultAsync(t => t.Id == id, ct);

    public async Task<IReadOnlyList<TaskItem>> ListByProjectAsync(
        Guid projectId,
        DomainTaskStatus? statusFilter,
        CancellationToken ct = default)
    {
        var query = _db.Tasks
            .AsNoTracking()
            .AsSplitQuery()// 1query=>1tags
            .Include(t => t.Tags)
            .Where(t => t.ProjectId == projectId);

        if (statusFilter is not null)
            query = query.Where(t => t.Status == statusFilter.Value);

        return await query.OrderBy(t => t.CreatedAtUtc).ToListAsync(ct);
    }

    public async Task AddAsync(TaskItem task, CancellationToken ct = default)
    {
        await _db.Tasks.AddAsync(task, ct);
    }

    public Task<Tag?> GetTagByNameAsync(string name, CancellationToken ct = default)
    {
        var normalized = name.Trim().ToLowerInvariant();
        return _db.Tags.FirstOrDefaultAsync(t => t.Name == normalized, ct);
    }

    public async Task<Tag> GetOrCreateTagAsync(string name, CancellationToken ct = default)
    {
        var normalized = name.Trim().ToLowerInvariant();

        var existing = await _db.Tags.FirstOrDefaultAsync(t => t.Name == normalized, ct);
        if (existing is not null)
            return existing;

        var tag = new Tag(normalized);
        _db.Tags.Add(tag);
        try
        {
            await _db.SaveChangesAsync(ct);
            return tag;
        }
        catch (DbUpdateException)
        {
            // A concurrent request inserted the same tag first (unique index on Name).
            // Detach our losing duplicate and reuse the row that won the race.
            _db.Entry(tag).State = EntityState.Detached;
            return await _db.Tags.FirstAsync(t => t.Name == normalized, ct);
        }
    }

    public void Remove(TaskItem task)
    {
        _db.Tasks.Remove(task);
    }
}
