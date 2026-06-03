using TaskTracker.Application.Users.Dtos;
using TaskTracker.Application.Users.Services;

namespace TaskTracker.Api.Endpoints;

public static class UserEndpoints
{
    public static IEndpointRouteBuilder MapUserEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/users").WithTags("Users");
        /* Dummy chaning in project... */
        group.MapPost("/", async (CreateUserRequest request, IUserService service, CancellationToken ct) =>
        {
            var created = await service.CreateAsync(request, ct);
            return Results.Created($"/users/{created.Id}", created);
        });

        group.MapGet("/{id:guid}", async (Guid id, IUserService service, CancellationToken ct) =>
        {
            var user = await service.GetByIdAsync(id, ct);
            return Results.Ok(user);
        });

        group.MapGet("/", async (IUserService service, CancellationToken ct) =>
        {
            var users = await service.ListAsync(ct);
            return Results.Ok(users);
        });

        return app;
    }
}
