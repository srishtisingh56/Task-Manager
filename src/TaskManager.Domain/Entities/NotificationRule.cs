using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using TaskManager.Domain.Common;
using TaskManager.Domain.Enums;
using TaskManager.Domain.Exceptions;

namespace TaskManager.Domain.Entities
{
    public sealed class NotificationRule : Entity
    {
        public Guid TaskId { get; private set; }
        // public NotificationChannel Channel { get; private set; }
        public NotificationTriggerEvent TriggerEvent { get; private set; }
        public NotificationOffsetUnit? OffsetUnit { get; private set; }
        public int? OffsetValue { get; private set; }
        public NotificationRepeatMode RepeatMode { get; private set; }

        private static readonly HashSet<NotificationTriggerEvent> OffsetBasedTriggers = new()
        {
            NotificationTriggerEvent.AfterCreationOffset,
            NotificationTriggerEvent.BeforeLenientDeadline,
            NotificationTriggerEvent.BeforeStrictDeadline
        };

        private NotificationRule()
        {
        }

        private NotificationRule(
            Guid id,
            Guid taskId,
            // NotificationChannel channel,
            NotificationTriggerEvent triggerEvent,
            NotificationOffsetUnit? offsetUnit,
            int? offsetValue,
            NotificationRepeatMode repeatMode
        ) : base(id)
        {
            TaskId = taskId;
            // Channel = channel;
            TriggerEvent = triggerEvent;
            OffsetUnit = offsetUnit;
            OffsetValue = offsetValue;
            RepeatMode = repeatMode;
        }

        private static void ValidateTriggerEvent(
            NotificationTriggerEvent triggerEvent,
            NotificationOffsetUnit? offsetUnit,
            int? offsetValue,
            NotificationRepeatMode repeatMode
        )
        {
            bool requireOffset = OffsetBasedTriggers.Contains(triggerEvent);
            if (requireOffset)
            {
                if (offsetValue is null or <= 0 || offsetUnit is null)
                    throw new InvalidNotificationRuleException(
                        $"TriggerEvent '{triggerEvent}' requires a positive OffsetValue and an OffsetUnit.");

                // Only "after creation" reminders may repeat; deadline warnings fire once.
                if (repeatMode == NotificationRepeatMode.Repeat && triggerEvent != NotificationTriggerEvent.AfterCreationOffset)
                    throw new InvalidNotificationRuleException(
                        $"TriggerEvent '{triggerEvent}' cannot use RepeatMode.Repeat.");
            }
            else
            {
                throw new InvalidNotificationRuleException(
                    $"Immediate events cannot be scheduled with an offset.");
            }
        }
        
        public static NotificationRule Create(
            Guid taskId,
            // NotificationChannel channel,
            NotificationTriggerEvent triggerEvent,
            NotificationOffsetUnit? offsetUnit,
            int? offsetValue,
            NotificationRepeatMode repeatMode
        )
        {
            if(taskId == Guid.Empty)
                throw new ArgumentException("TaskId cannot be empty.", nameof(taskId));

            ValidateTriggerEvent(triggerEvent,offsetUnit,offsetValue,repeatMode);
                        
            return new NotificationRule(
                Guid.NewGuid(),
                taskId,
                // channel,
                triggerEvent,
                offsetUnit,
                offsetValue,
                repeatMode
            );
        }

        public void UpdateSchedule(
            NotificationTriggerEvent triggerEvent,
            NotificationOffsetUnit? offsetUnit,
            int? offsetValue,
            NotificationRepeatMode repeatMode
        )
        {
            ValidateTriggerEvent(triggerEvent, offsetUnit, offsetValue, repeatMode);
            TriggerEvent = triggerEvent;
            OffsetUnit = offsetUnit;
            OffsetValue = offsetValue;
            RepeatMode = repeatMode;
        }
        
        public DateTime? CalculateFireTime(DateTime referencePointUtc)
        {
            if(OffsetUnit is null || OffsetValue is null)
                return null;
            
            TimeSpan offset = OffsetUnit.Value switch
            {
                NotificationOffsetUnit.Hours => TimeSpan.FromHours(OffsetValue.Value),
                NotificationOffsetUnit.Days => TimeSpan.FromDays(OffsetValue.Value),
                NotificationOffsetUnit.Week => TimeSpan.FromDays(7 * OffsetValue.Value),
                _ => throw new InvalidNotificationRuleException($"Unknown OffsetUnit '{OffsetUnit.Value}'.")
            };

            return TriggerEvent switch
            {
                NotificationTriggerEvent.BeforeLenientDeadline => referencePointUtc - offset,
                NotificationTriggerEvent.BeforeStrictDeadline => referencePointUtc - offset,
                NotificationTriggerEvent.AfterCreationOffset => referencePointUtc + offset,
                _ => throw new InvalidNotificationRuleException($"Unsupported trigger event '{TriggerEvent}'.")
            };
        }
    }
}
