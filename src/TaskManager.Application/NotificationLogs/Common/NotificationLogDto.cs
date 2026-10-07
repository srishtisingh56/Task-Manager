using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using TaskManager.Domain.Enums;

namespace TaskManager.Application.NotificationLogs.Common
{
   public sealed record NotificationLogDto(
    Guid Id, 
    Guid TaskId, 
    string TaskTitle,
    NotificationTriggerEvent TriggerEvent, 
    NotificationChannel Channel,
    NotificationDeliveryStatus DeliveryStatus, 
    DateTime SentAt
    );
}