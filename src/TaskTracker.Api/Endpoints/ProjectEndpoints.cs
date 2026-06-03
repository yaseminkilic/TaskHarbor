using TaskTracker.Application.Projects.Dtos;
using TaskTracker.Application.Projects.Services;

namespace TaskTracker.Api.Endpoints;

public static class ProjectEndpoints
{
    public static IEndpointRouteBuilder MapProjectEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/projects").WithTags("Projects");

        group.MapPost("/", async (CreateProjectRequest request, IProjectService service, CancellationToken ct) =>
        {
            var created = await service.CreateAsync(request, ct);
            return Results.Created($"/projects/{created.Id}", created);
        });

        group.MapGet("/{id:guid}", async (Guid id, IProjectService service, CancellationToken ct) =>
        {
            var project = await service.GetByIdAsync(id, ct);
            return Results.Ok(project);
        });

        app.MapGet("/users/{ownerId:guid}/projects",
            async (Guid ownerId, IProjectService service, CancellationToken ct) =>
            {
                var projects = await service.ListByOwnerAsync(ownerId, ct);
                return Results.Ok(projects);
            })
            .WithTags("Projects");

        return app;
    }
}
