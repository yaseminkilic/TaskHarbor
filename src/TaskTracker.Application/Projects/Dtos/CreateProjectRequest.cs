namespace TaskTracker.Application.Projects.Dtos;

public record CreateProjectRequest(Guid OwnerId, string Name);
