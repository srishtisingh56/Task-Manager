using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using FluentValidation;
namespace TaskManager.Application.Tasks.Queries.GetTaskById
{
    public class GetTaskByIdQueryValidator : AbstractValidator<GetTaskByIdQuery>
    {
        public GetTaskByIdQueryValidator()
        {
          RuleFor(x => x.TaskId)
            .NotEmpty()
            .WithMessage("Task ID is required."); 
        }
    }
}