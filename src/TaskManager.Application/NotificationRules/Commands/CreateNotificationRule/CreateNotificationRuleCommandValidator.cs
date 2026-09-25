
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
             .WithMessage("Invalid Notification Repeat Mode")
             .Must((command, repeatMode) => repeatMode != NotificationRepeatMode.Repeat
                                             || command.TriggerEvent == NotificationTriggerEvent.AfterCreationOffset)
             .WithMessage("Only the 'AfterCreationOffset' trigger event can use RepeatMode.Repeat.");

            RuleFor(x => x.OffsetValue)
            .NotNull()
            .WithMessage("Offset value must not be null.")
            .GreaterThan(0)
            .WithMessage("Offset value must be greater than 0.");

            RuleFor(x => x.OffsetUnit)
            .NotNull()
            .WithMessage("Offset unit must not be null.")
            .IsInEnum()
            .WithMessage("Invalid Offset Unit.");
        }
    }   
}