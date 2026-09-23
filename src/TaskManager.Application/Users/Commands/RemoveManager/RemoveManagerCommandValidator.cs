using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using FluentValidation;

namespace TaskManager.Application.Users.Commands.RemoveManager
{
    public sealed class RemoveManagerCommandValidator : AbstractValidator<RemoveManagerCommand>
    {
        public RemoveManagerCommandValidator()
        {
            RuleFor(x => x.WorkerId).NotEmpty().WithMessage("Worker Id is required.");
        }
    }
}