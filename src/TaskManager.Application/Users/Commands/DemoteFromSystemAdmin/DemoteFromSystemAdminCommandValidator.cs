using FluentValidation;

namespace TaskManager.Application.Users.Commands.DemoteFromSystemAdmin
{
    public sealed class DemoteFromSystemAdminCommandValidator : AbstractValidator<DemoteFromSystemAdminCommand>
    {
        public DemoteFromSystemAdminCommandValidator()
        {
            RuleFor(x => x.UserId)
            .NotEmpty().WithMessage("User Id is required.");
        }
    }
}