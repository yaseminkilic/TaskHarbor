using FluentValidation.Results;

namespace TaskTracker.Application.Common.Exceptions;

public class ValidationException : Exception
{
    public ValidationException(IEnumerable<ValidationFailure> failures)
        : base("One or more validation errors occurred.")
    {
        Errors = failures
            .GroupBy(f => f.PropertyName)
            .ToDictionary(
                g => g.Key,
                g => g.Select(f => f.ErrorMessage).ToArray());
    }

    public ValidationException(string propertyName, string message)
        : base("One or more validation errors occurred.")
    {
        Errors = new Dictionary<string, string[]>
        {
            [propertyName] = new[] { message }
        };
    }

    public IReadOnlyDictionary<string, string[]> Errors { get; }
}
