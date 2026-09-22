using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using FluentValidation;

namespace TaskManager.Application.Tasks.Commands.ReassignTask
{
    public sealed class ReassignTaskCommandValidator : AbstractValidator<ReassignTaskCommand>
    {
        public ReassignTaskCommandValidator()
        {
            RuleFor(x => x.TaskId)
            .NotEmpty()
            .WithMessage("Task ID is required.");
            
            RuleFor(x => x.NewAssigneeId)
            .NotEmpty()
            .WithMessage("New assignee ID is required.");
        }
    }
}