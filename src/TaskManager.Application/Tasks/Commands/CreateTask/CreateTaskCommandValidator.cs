using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using FluentValidation;
namespace TaskManager.Application.Tasks.Commands.CreateTask
{
    public sealed class CreateTaskCommandValidator : AbstractValidator<CreateTaskCommand>
    {
        public CreateTaskCommandValidator()
        {
            RuleFor(x=>x.Title)
                .NotEmpty()
                .MaximumLength(200);

            RuleFor(x=>x.Description)
                .MaximumLength(2000);

            RuleFor(x=>x.StrictDeadline)
                .GreaterThan(x=>x.LenientDeadline)
                .WithMessage("Strict deadline must be greater than lenient deadline.");
            
            RuleFor(x=>x.AssignedToUserId)
                .NotEmpty();
        }
    }
}