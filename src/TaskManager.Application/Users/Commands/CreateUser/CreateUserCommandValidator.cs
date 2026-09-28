using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using FluentValidation;
namespace TaskManager.Application.Users.Commands.CreateUser
{
    public sealed class CreateUserCommandValidator : AbstractValidator<CreateUserCommand>
    {
        public CreateUserCommandValidator()
        {
            RuleFor(x => x.Name)
            .NotEmpty()
            .WithMessage("Name must not be empty")
            .MaximumLength(200)
            .WithMessage("Name must not exceed 200 characters");

            RuleFor(x => x.Email)
            .NotEmpty()
            .WithMessage("Email must not be empty")
            .EmailAddress()
            .MaximumLength(320)
            .WithMessage("Email must not exceed 320 characters");

            RuleFor(x => x.PhoneNumber)
            .NotEmpty()
            .WithMessage("Phone number must not be empty")
            .MaximumLength(10)
            .WithMessage("Phone number must not exceed 10 characters");

            RuleFor(x => x.Password)
            .NotEmpty()
            .WithMessage("Password must not be empty")
            .MinimumLength(8)
            .WithMessage("Password must be at least 8 characters long")
            .MaximumLength(100)
            .WithMessage("Password must not exceed 100 characters");
        }
    }

}