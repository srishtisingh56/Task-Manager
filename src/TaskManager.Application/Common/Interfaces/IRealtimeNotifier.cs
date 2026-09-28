using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using TaskManager.Domain.Enums;

namespace TaskManager.Application.Common.Interfaces
{
    public interface IRealtimeNotifier
    {
        Task NotifyAsync(Guid recipientUserId, Guid taskId, NotificationTriggerEvent triggerEvent,
            DateTime sentAtUtc, CancellationToken ct);
    }
}