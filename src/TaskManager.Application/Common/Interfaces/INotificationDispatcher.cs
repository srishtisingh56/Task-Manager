using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using TaskManager.Domain.Enums;

namespace TaskManager.Application.Common.Interfaces
{
    public interface INotificationDispatcher
    {
        Task DispatchAsync(
            Guid taskId,
            Guid recipientUserId,
            NotificationTriggerEvent triggerEvent,
            CancellationToken ct
        );
    }
}