// Users/Commands/ReactivateUser/ReactivateUserCommandValidator.cs
using FluentValidation;

namespace TaskManager.Application.Users.Commands.ReactivateUser;

public sealed class ReactivateUserCommandValidator : AbstractValidator<ReactivateUserCommand>
{
    public ReactivateUserCommandValidator()
    {
        RuleFor(x => x.UserId)
        .NotEmpty().WithMessage("User Id cannot be empty.");
    }
}