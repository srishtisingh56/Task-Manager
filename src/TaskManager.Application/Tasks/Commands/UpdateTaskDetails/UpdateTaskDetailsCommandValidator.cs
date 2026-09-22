using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using FluentValidation;

namespace TaskManager.Application.Tasks.Commands.UpdateTaskDetails
{
    public sealed class UpdateTaskDetailsCommandValidator : AbstractValidator<UpdateTaskDetailsCommand>
    {
        public UpdateTaskDetailsCommandValidator()
        {
            RuleFor(x=>x.TaskId)
                .NotEmpty()
                .WithMessage("TaskId is required.");

            When(x => x.Title.IsSet, () =>
            {
                RuleFor(x=>x.Title.Value)
                .NotEmpty()
                .MaximumLength(200)
                .WithMessage("Title must not exceed 200 characters.");
                
            });

            When(x => x.Description.IsSet, () =>
            {
                RuleFor(x=>x.Description.Value)
                .MaximumLength(1000)
                .WithMessage("Description must not exceed 1000 characters.");
                //no NotEmpty() -> description can be set to null if IsSet is true (this was required to implement using Optional<>)
            });

        }
        
    }
}