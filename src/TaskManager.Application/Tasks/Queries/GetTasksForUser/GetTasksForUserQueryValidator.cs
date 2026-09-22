using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using FluentValidation;

namespace TaskManager.Application.Tasks.Queries.GetTasksForUser
{
    public sealed class GetTasksForUserQueryValidator : AbstractValidator<GetTasksForUserQuery>
    {
        public GetTasksForUserQueryValidator()
        {
            RuleFor(x => x.PageNumber)
                .GreaterThan(0)
                .WithMessage("Page number must be greater than 0.");

            RuleFor(x => x.PageSize)
                .InclusiveBetween(1,100)
                .WithMessage("Page size must be between 1 and 100.");

            When(x=>x.Status.HasValue,()=> {
                RuleFor(x => x.Status!.Value)
                    .IsInEnum()
                    .WithMessage("Invalid status value.");
            });
        }
    }
}