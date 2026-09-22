
using FluentValidation;
namespace TaskManager.Application.Tasks.Commands.RestoreTask
{
public sealed class RestoreTaskCommandValidator : AbstractValidator<RestoreTaskCommand>
    {
        public RestoreTaskCommandValidator()
        {
            RuleFor(x => x.TaskId)
                .NotEmpty()
                .WithMessage("Task ID is required.");
        }
    }
}