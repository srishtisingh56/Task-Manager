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
            
            RuleFor(x => x.TriggerEvent)
             .IsInEnum()
             .WithMessage("Invalid Notification Event.")
             .Must(t=>NotificationTriggerEvents.OffsetBased.Contains(t))
             .WithMessage("Only offset-based trigger events can be configured as a notification rule. " +
                          "Immediate events are dispatched automatically.");
            
            RuleFor(x => x.RepeatMode)
             .IsInEnum()
             .WithMessage("Invalid Notification Repeat Mode");
            
            When(x => x.OffsetValue.IsSet, () =>
            {
                RuleFor(x => x.OffsetValue.Value)
                 .GreaterThan(0)
                 .WithMessage("Offset value must be greater than 0.");
            });
            // RuleFor(x => x.OffsetValue)
            // .GreaterThan(0)
            // .WithMessage("Offset value must be greater than 0.");

            RuleFor(x => x.OffsetUnit)
            .IsInEnum()
            .WithMessage("Invalid Offset Unit.");
        }
    }   
}