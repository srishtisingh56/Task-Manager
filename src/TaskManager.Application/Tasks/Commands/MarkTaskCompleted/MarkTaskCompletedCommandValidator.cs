using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using FluentValidation;

namespace TaskManager.Application.Tasks.Commands.MarkTaskCompleted
{
    public sealed class MarkTaskCompletedCommandValidator : AbstractValidator<MarkTaskCompletedCommand>
    {
        public MarkTaskCompletedCommandValidator()
        {
            RuleFor(x => x.TaskId)
            .NotEmpty()
            .WithMessage("Task ID is required.");
        }
    }
}