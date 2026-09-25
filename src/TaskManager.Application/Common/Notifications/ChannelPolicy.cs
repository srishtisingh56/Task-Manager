using System;
using System.Collections.Generic;
using TaskManager.Domain.Enums;

namespace TaskManager.Application.Common.Notifications
{
    /// <summary>
    /// Channels used for immediate/system events. Offset-based (scheduled) triggers
    /// use the channel configured on their NotificationRule instead of this policy.
    /// </summary>
    public static class ChannelPolicy
    {
        private static readonly IReadOnlyDictionary<NotificationTriggerEvent, IReadOnlyList<NotificationChannel>> Channels =
            new Dictionary<NotificationTriggerEvent, IReadOnlyList<NotificationChannel>>
            {
                [NotificationTriggerEvent.Created] = new[] { NotificationChannel.App },
                [NotificationTriggerEvent.Updated] = new[] { NotificationChannel.App },
                [NotificationTriggerEvent.Deleted] = new[] { NotificationChannel.App },
                [NotificationTriggerEvent.Completed] = new[] { NotificationChannel.App, NotificationChannel.Email },
                [NotificationTriggerEvent.StrictDeadlinePassed] = new[] { NotificationChannel.App, NotificationChannel.Email },
                [NotificationTriggerEvent.AfterCreationOffset] = new[] { NotificationChannel.App },
                [NotificationTriggerEvent.BeforeLenientDeadline] = new[] { NotificationChannel.App },
                [NotificationTriggerEvent.BeforeStrictDeadline] = new[] { NotificationChannel.App },
            };

        public static IReadOnlyList<NotificationChannel> GetChannels(NotificationTriggerEvent triggerEvent)
        {
            if (!Channels.TryGetValue(triggerEvent, out var channels))
                throw new ArgumentOutOfRangeException(nameof(triggerEvent),
                    $"No channel policy defined for '{triggerEvent}'.");

            return channels;
        }
    }
}
