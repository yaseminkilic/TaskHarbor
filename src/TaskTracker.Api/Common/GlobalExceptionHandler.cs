using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using TaskTracker.Application.Common.Exceptions;
using TaskTracker.Domain.Common;
using AppValidationException = TaskTracker.Application.Common.Exceptions.ValidationException;

namespace TaskTracker.Api.Common;

public class GlobalExceptionHandler : IExceptionHandler
{
    private readonly IProblemDetailsService _problemDetailsService;
    private readonly ILogger<GlobalExceptionHandler> _logger;

    public GlobalExceptionHandler(
        IProblemDetailsService problemDetailsService,
        ILogger<GlobalExceptionHandler> logger)
    {
        _problemDetailsService = problemDetailsService;
        _logger = logger;
    }

    public async ValueTask<bool> TryHandleAsync(
        HttpContext httpContext,
        Exception exception,
        CancellationToken cancellationToken)
    {
        var problem = Map(exception);
        httpContext.Response.StatusCode = problem.Status ?? StatusCodes.Status500InternalServerError;

        if (problem.Status >= 500)
            _logger.LogError(exception, "Unhandled exception");
        else
            _logger.LogWarning(exception, "Handled exception: {Type}", exception.GetType().Name);

        return await _problemDetailsService.TryWriteAsync(new ProblemDetailsContext
        {
            HttpContext = httpContext,
            ProblemDetails = problem,
            Exception = exception
        });
    }

    private static ProblemDetails Map(Exception exception) => exception switch
    {
        NotFoundException nf => new ProblemDetails
        {
            Title = "Resource not found",
            Status = StatusCodes.Status404NotFound,
            Detail = nf.Message,
            Type = "https://datatracker.ietf.org/doc/html/rfc7231#section-6.5.4"
        },
        ConflictException c => new ProblemDetails
        {
            Title = "Conflict",
            Status = StatusCodes.Status409Conflict,
            Detail = c.Message,
            Type = "https://datatracker.ietf.org/doc/html/rfc7231#section-6.5.8"
        },
        AppValidationException v => new ValidationProblemDetails(v.Errors.ToDictionary(kv => kv.Key, kv => kv.Value))
        {
            Title = "Validation failed",
            Status = StatusCodes.Status400BadRequest,
            Type = "https://datatracker.ietf.org/doc/html/rfc7231#section-6.5.1"
        },
        DomainException d => new ProblemDetails
        {
            Title = "Domain rule violation",
            Status = StatusCodes.Status400BadRequest,
            Detail = d.Message,
            Type = "https://datatracker.ietf.org/doc/html/rfc7231#section-6.5.1"
        },
        _ => new ProblemDetails
        {
            Title = "An unexpected error occurred",
            Status = StatusCodes.Status500InternalServerError,
            Detail = "See server logs for details.",
            Type = "https://datatracker.ietf.org/doc/html/rfc7231#section-6.6.1"
        }
    };
}
