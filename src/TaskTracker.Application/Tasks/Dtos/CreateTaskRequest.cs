namespace TaskTracker.Application.Tasks.Dtos;

public record CreateTaskRequest(Guid ProjectId, string Title, string? Description);
