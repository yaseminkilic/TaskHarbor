using FluentValidation;
using TaskTracker.Application.Projects.Dtos;

namespace TaskTracker.Application.Projects.Validators;

public class CreateProjectRequestValidator : AbstractValidator<CreateProjectRequest>
{
    public CreateProjectRequestValidator()
    {
        RuleFor(x => x.OwnerId).NotEmpty();
        RuleFor(x => x.Name)
            .NotEmpty()
            .MaximumLength(160);
    }
}
