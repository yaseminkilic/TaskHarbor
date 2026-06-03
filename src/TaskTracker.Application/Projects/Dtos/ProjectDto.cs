using TaskTracker.Domain.Entities;

namespace TaskTracker.Application.Projects.Dtos;
// ...
public record ProjectDto(
    Guid Id,
    string Name,
    Guid OwnerId,
    DateTime CreatedAtUtc,
    int TaskCount)
{
    public static ProjectDto FromEntity(Project p) =>
        new(p.Id, p.Name, p.OwnerId, p.CreatedAtUtc, p.Tasks.Count);
}
