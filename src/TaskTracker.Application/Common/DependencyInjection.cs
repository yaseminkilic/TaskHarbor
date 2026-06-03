using System.Reflection;
using FluentValidation;
using Microsoft.Extensions.DependencyInjection;
using TaskTracker.Application.Projects.Services;
using TaskTracker.Application.Tasks.Services;
using TaskTracker.Application.Users.Services;

namespace TaskTracker.Application.Common;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddValidatorsFromAssembly(Assembly.GetExecutingAssembly());

        services.AddScoped<IUserService, UserService>();
        services.AddScoped<IProjectService, ProjectService>();
        services.AddScoped<ITaskService, TaskService>();

        return services;
    }
}
