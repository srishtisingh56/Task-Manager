
using FluentValidation;

namespace TaskManager.Application.Users.Commands.PromoteToSystemAdmin
{
  public sealed class PromoteToSystemAdminCommandValidator : AbstractValidator<PromoteToSystemAdminCommand>
    {
        public PromoteToSystemAdminCommandValidator()
        {
            RuleFor(x => x.UserId).NotEmpty().WithMessage("User Id is required.");
        }
    }
}