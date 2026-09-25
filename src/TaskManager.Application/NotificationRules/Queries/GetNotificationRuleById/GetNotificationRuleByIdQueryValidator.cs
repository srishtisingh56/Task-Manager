using FluentValidation;

namespace TaskManager.Application.NotificationRules.Queries.GetNotificationRuleById
{
    public sealed class GetNotificationRuleByIdQueryValidator : AbstractValidator<GetNotificationRuleByIdQuery>
    {
        public GetNotificationRuleByIdQueryValidator()
        {
            RuleFor(x => x.RuleId)
             .NotEmpty()
             .WithMessage("Rule Id is required.");
        }
    }
}
