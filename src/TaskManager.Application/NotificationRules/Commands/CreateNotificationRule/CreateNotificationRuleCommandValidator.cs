
using FluentValidation;
using TaskManager.Application.NotificationRules.Common;
using TaskManager.Domain.Enums;
namespace TaskManager.Application.NotificationRules.Commands.CreateNotificationRule
{
    public sealed class CreateNotificationRuleCommandValidator : AbstractValidator<CreateNotificationRuleCommand>
    {
        public CreateNotificationRuleCommandValidator()
        {
            RuleFor(x => x.TaskId)
             .NotEmpty()
             .WithMessage("Task Id must not be empty.");
            
            RuleFor(x => x.TriggerEvent)
             .IsInEnum()
             .WithMessage("Invalid Notification Event.")
             .Must(t=>NotificationTriggerEvents.OffsetBased.Contains(t))
             .WithMessage("Only offset-based trigger events can be configured as a notification rule. " +
                          "Immediate events are dispatched automatically.");
            
            RuleFor(x => x.RepeatMode)
             .IsInEnum()
             .WithMessage("Invalid Notification Repeat Mode");
            
            RuleFor(x => x.Channel)
             .IsInEnum()
             .WithMessage("Invalid Notification Channel");

            RuleFor(x => x.OffsetValue)
            .GreaterThan(0)
            .WithMessage("Offset value must be greater than 0.");

            RuleFor(x => x.OffsetUnit)
            .IsInEnum()
            .WithMessage("Invalid Offset Unit.");
        }
    }   
}