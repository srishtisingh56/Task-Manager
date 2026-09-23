using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using FluentValidation;

namespace TaskManager.Application.Users.Commands.AssignManager
{
    public sealed class AssignManagerCommandValidator : AbstractValidator<AssignManagerCommand>
    {
        public AssignManagerCommandValidator()
        {
            RuleFor(x => x.WorkerId).NotEmpty().WithMessage("Worker Id must not be empty");
            RuleFor(x => x.ManagerId).NotEmpty().WithMessage("Manager Id must not be empty");
        }
    }
}