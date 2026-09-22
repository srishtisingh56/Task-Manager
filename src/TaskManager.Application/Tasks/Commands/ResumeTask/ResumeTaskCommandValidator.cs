using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using FluentValidation;

namespace TaskManager.Application.Tasks.Commands.ResumeTask
{
    public class ResumeTaskCommandValidator : AbstractValidator<ResumeTaskCommand>
    {
        public ResumeTaskCommandValidator()
        {
            RuleFor(x => x.TaskId)
                .NotEmpty()
                .WithMessage("Task ID is required.");
        }
    }
}