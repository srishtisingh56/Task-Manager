using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using TaskManager.Domain.Enums;

namespace TaskManager.Application.NotificationRules.Common
{
    public static class NotificationTriggerEvents
    {
        public static readonly IReadOnlySet<NotificationTriggerEvent> OffsetBased =
            new HashSet<NotificationTriggerEvent>
        {   
            NotificationTriggerEvent.AfterCreationOffset,
            NotificationTriggerEvent.BeforeLenientDeadline,
            NotificationTriggerEvent.BeforeStrictDeadline
        };
    }
}