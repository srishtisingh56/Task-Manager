// using System;
// using System.Collections.Generic;
// using System.Linq;
// using System.Threading.Tasks;
// using Microsoft.AspNetCore.SignalR;
// using TaskManager.Application.Common.Interfaces;
// using TaskManager.Domain.Enums;

// namespace TaskManager.Presentation.Hubs
// {
//     public sealed class SignalRRealtimeNotifier(IHubContext<NotificationHub> hub) : IRealtimeNotifier
// {
//     public Task NotifyAsync(Guid recipientUserId, Guid taskId, NotificationTriggerEvent triggerEvent,
//         DateTime sentAtUtc, CancellationToken ct) =>
//         hub.Clients.Group(NotificationHub.GroupFor(recipientUserId))
//             .SendAsync("notification",
//                 new { taskId, triggerEvent = triggerEvent.ToString(), sentAt = sentAtUtc }, ct);
// }
// }