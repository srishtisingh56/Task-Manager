using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using FluentValidation;

namespace TaskManager.Application.Tasks.Commands.DeleteTasksByStatus
{
    public sealed class DeleteTasksByStatusCommandValidator : AbstractValidator<DeleteTasksByStatusCommand>
    {
        public DeleteTasksByStatusCommandValidator()
        {
            RuleFor(x => x.Status)
                .IsInEnum()
                .WithMessage("Invalid task status.");
        }
    }
}