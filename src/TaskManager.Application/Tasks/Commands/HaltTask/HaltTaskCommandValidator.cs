using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using FluentValidation;
namespace TaskManager.Application.Tasks.Commands.HaltTask
{
    public sealed class HaltTaskCommandValidator : AbstractValidator<HaltTaskCommand>
    {
        public HaltTaskCommandValidator()
        {
            RuleFor(x => x.TaskId)
            .NotEmpty()
            .WithMessage("Task ID is required.");
        }
    }
}