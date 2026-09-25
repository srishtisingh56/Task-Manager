using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using FluentValidation;
using TaskManager.Application.NotificationRules.Common;
using TaskManager.Domain.Enums;

namespace TaskManager.Application.NotificationRules.Commands.UpdateSchedule
{
    public sealed class UpdateScheduleCommandValidator : AbstractValidator<UpdateScheduleCommand>
    {
        
        public UpdateScheduleCommandValidator()
        {
            When(x => x.TriggerEvent.IsSet, () =>
            {
                RuleFor(x => x.TriggerEvent.Value)
                 .IsInEnum()
                 .WithMessage("Invalid Notification Event.")
                 .Must(t => NotificationTriggerEvents.OffsetBased.Contains(t))
                 .WithMessage("Only offset-based trigger events can be configured as a notification rule. " +
                              "Immediate events are dispatched automatically.");
            });

            When(x => x.RepeatMode.IsSet, () =>
            {
                RuleFor(x => x.RepeatMode.Value)
                 .IsInEnum()
                 .WithMessage("Invalid Notification Repeat Mode");
            });

            // Cross-field check: RepeatMode.Repeat valid only for AfterCreationOffset.
            // Only validate when both are provided in this request; entity guard handles existing value case.
            When(x => x.TriggerEvent.IsSet && x.RepeatMode.IsSet, () =>
            {
                RuleFor(x => x)
                 .Must(x => x.RepeatMode.Value != NotificationRepeatMode.Repeat
                            || x.TriggerEvent.Value == NotificationTriggerEvent.AfterCreationOffset)
                 .WithMessage("RepeatMode.Repeat is only valid for AfterCreationOffset trigger events.");
            });

            When(x => x.OffsetValue.IsSet, () =>
            {
                RuleFor(x => x.OffsetValue.Value)
                 .NotNull()
                 .WithMessage("Offset value must not be null.")
                 .GreaterThan(0)
                 .WithMessage("Offset value must be greater than 0.");
            });

            When(x => x.OffsetUnit.IsSet, () =>
            {
                RuleFor(x => x.OffsetUnit.Value)
                 .NotNull()
                 .WithMessage("Offset unit must not be null.")
                 .IsInEnum()
                 .WithMessage("Invalid Offset Unit.");
            });
        }
    }   
}