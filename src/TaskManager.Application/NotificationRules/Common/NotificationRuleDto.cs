using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using TaskManager.Domain.Enums;

namespace TaskManager.Application.NotificationRules.Common
{
    public sealed record NotificationRuleDto
    (
        Guid Id,
        Guid TaskId,
        NotificationTriggerEvent TriggerEvent,
        int? OffsetValue,
        NotificationOffsetUnit? OffsetUnit,
        NotificationRepeatMode RepeatMode
    )
    {
        public static NotificationRuleDto FromEntity(Domain.Entities.NotificationRule rule) =>
            new NotificationRuleDto(
                rule.Id,
                rule.TaskId,
                rule.TriggerEvent,
                rule.OffsetValue,
                rule.OffsetUnit,
                rule.RepeatMode
            );
    }
}