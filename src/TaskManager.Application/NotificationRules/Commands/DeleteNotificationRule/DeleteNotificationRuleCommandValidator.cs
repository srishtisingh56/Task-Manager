using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using FluentValidation;
namespace TaskManager.Application.NotificationRules.Commands.DeleteNotificationRule
{
    public sealed class DeleteNotificationRuleCommandValidator : AbstractValidator<DeleteNotificationRuleCommand>
    {
        public DeleteNotificationRuleCommandValidator()
        {
            RuleFor(x => x.RuleId)
            .NotEmpty().WithMessage("Rule Id is required");
        }
    }
}