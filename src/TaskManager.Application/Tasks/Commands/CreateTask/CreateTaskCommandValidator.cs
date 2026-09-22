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
                .MaximumLength(200)
                .WithMessage("Title must not exceed 200 characters.");

            RuleFor(x=>x.Description)
                .MaximumLength(1000)
                .WithMessage("Description must not exceed 1000 characters.");

            RuleFor(x=>x.LenientDeadline)
                .NotEqual(default(DateTime))
                .WithMessage("Lenient deadline must be a valid date.");

            RuleFor(x=>x.StrictDeadline)
                .GreaterThan(x=>x.LenientDeadline)
                .WithMessage("Strict deadline must be greater than lenient deadline.");
            
            RuleFor(x=>x.AssignedToUserId)
                .NotEmpty()
                .WithMessage("Task assignee ID is required.");
        }
    }
}