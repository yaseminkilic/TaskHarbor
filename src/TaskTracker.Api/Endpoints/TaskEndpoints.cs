using TaskTracker.Application.Tasks.Dtos;
using TaskTracker.Application.Tasks.Services;

namespace TaskTracker.Api.Endpoints;

public static class TaskEndpoints
{
    public record AddTagBody(string Name);

    public static IEndpointRouteBuilder MapTaskEndpoints(this IEndpointRouteBuilder app)
    {
        var tasks = app.MapGroup("/tasks").WithTags("Tasks");

        tasks.MapGet("/{id:guid}", async (Guid id, ITaskService service, CancellationToken ct) =>
            Results.Ok(await service.GetByIdAsync(id, ct)));

        tasks.MapPatch("/{id:guid}", async (Guid id, UpdateTaskRequest body, ITaskService service, CancellationToken ct) =>
            Results.Ok(await service.UpdateAsync(id, body, ct)));

        tasks.MapDelete("/{id:guid}", async (Guid id, ITaskService service, CancellationToken ct) =>
        {
            await service.DeleteAsync(id, ct);
            return Results.NoContent();
        });

        tasks.MapPost("/{id:guid}/start", async (Guid id, ITaskService service, CancellationToken ct) =>
            Results.Ok(await service.StartAsync(id, ct)));

        tasks.MapPost("/{id:guid}/complete", async (Guid id, ITaskService service, CancellationToken ct) =>
            Results.Ok(await service.CompleteAsync(id, ct)));

        tasks.MapPost("/{id:guid}/tags", async (Guid id, AddTagBody body, ITaskService service, CancellationToken ct) =>
            Results.Ok(await service.AddTagAsync(id, body.Name, ct)));

        tasks.MapDelete("/{id:guid}/tags/{name}", async (Guid id, string name, ITaskService service, CancellationToken ct) =>
            Results.Ok(await service.RemoveTagAsync(id, name, ct)));

        app.MapPost("/projects/{projectId:guid}/tasks",
            async (Guid projectId, CreateTaskBody body, ITaskService service, CancellationToken ct) =>
            {
                var request = new CreateTaskRequest(projectId, body.Title, body.Description);
                var created = await service.CreateAsync(request, ct);
                return Results.Created($"/tasks/{created.Id}", created);
            })
            .WithTags("Tasks");

        app.MapGet("/projects/{projectId:guid}/tasks",
            async (Guid projectId, string? status, ITaskService service, CancellationToken ct) =>
                Results.Ok(await service.ListByProjectAsync(projectId, status, ct)))
            .WithTags("Tasks");

        return app;
    }

    public record CreateTaskBody(string Title, string? Description);
}
