using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using FluentValidation;
namespace TaskManager.Application.Tasks.Commands.UpdateTaskDeadlines
{
    public sealed class UpdateTaskDeadlinesCommandValidator : AbstractValidator<UpdateTaskDeadlinesCommand>
    {
        public UpdateTaskDeadlinesCommandValidator()
        {
            RuleFor(x => x.TaskId)
                .NotEmpty()
                .WithMessage("Task ID is required.");

            When(x => x.LenientDeadline.IsSet, () =>
            {
                RuleFor(x => x.LenientDeadline.Value)
                    .NotEqual(default(DateTime))
                    .WithMessage("Lenient deadline must be a valid date.");
            });

            When(x => x.StrictDeadline.IsSet, () =>
            {
                RuleFor(x => x.StrictDeadline.Value)
                    .NotEqual(default(DateTime))
                    .WithMessage("Strict deadline must be a valid date.");
            });
        }
    }
}