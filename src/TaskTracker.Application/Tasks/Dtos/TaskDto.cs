using TaskTracker.Domain.Entities;

namespace TaskTracker.Application.Tasks.Dtos;

public record TaskDto(
    Guid Id,
    Guid ProjectId,
    string Title,
    string? Description,
    string Status,
    DateTime CreatedAtUtc,
    DateTime? CompletedAtUtc,
    IReadOnlyList<string> Tags)
{
    public static TaskDto FromEntity(TaskItem t) =>
        new(
            t.Id,
            t.ProjectId,
            t.Title,
            t.Description,
            t.Status.ToString(),
            t.CreatedAtUtc,
            t.CompletedAtUtc,
            t.Tags.Select(tag => tag.Name).ToList());
}
