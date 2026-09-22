using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using TaskManager.Domain.Common;
using TaskManager.Domain.Enums;

namespace TaskManager.Domain.Entities
{
/// <summary>
/// An immutable record of a single notification send attempt: what was
/// sent, when, to whom, over which channel, and whether it succeeded.
/// </summary>
    public sealed class NotificationLog : Entity
    {
        public Guid NotificationRuleId { get; private set; }
        public Guid TaskId { get; private set; }
        public Guid RecipientUserId { get; private set; }
        public NotificationChannel Channel { get; private set; }
        public DateTime SentAt { get; private set; }
        public NotificationDeliveryStatus DeliveryStatus { get; private set; }

        private NotificationLog()
        {
        }

        private NotificationLog(
            Guid id,
            Guid notificationRuleId,
            Guid taskId,
            Guid recipientUserId, 
            NotificationChannel channel, 
            DateTime sentAt, 
            NotificationDeliveryStatus deliveryStatus)
            : base(id)
        {
            NotificationRuleId = notificationRuleId;
            TaskId = taskId;
            RecipientUserId = recipientUserId;
            Channel = channel;
            SentAt = sentAt;
            DeliveryStatus = deliveryStatus;
        }

        public static NotificationLog RecordAttempt(
            Guid notificationRuleId,
            Guid taskId,
            Guid recipientUserId, 
            NotificationChannel channel, 
            DateTime sentAt)
        {
            if(notificationRuleId == Guid.Empty)
                throw new ArgumentException("Notification rule ID cannot be empty.", nameof(notificationRuleId));
            if(taskId == Guid.Empty)
                throw new ArgumentException("Task ID cannot be empty.", nameof(taskId));
            if(recipientUserId == Guid.Empty)
                throw new ArgumentException("Recipient user ID cannot be empty.", nameof(recipientUserId));
            if(channel != NotificationChannel.Email && channel != NotificationChannel.Sms && channel != NotificationChannel.App)
                throw new ArgumentException("Invalid notification channel.", nameof(channel));
            
            return new NotificationLog(
                Guid.NewGuid(),
                notificationRuleId,
                taskId,
                recipientUserId,
                channel,
                sentAt,
                NotificationDeliveryStatus.Pending
            );
        }

        public void MarkSent()
        {
            DeliveryStatus = NotificationDeliveryStatus.Sent;
        }
        public void MarkFailed()
        {
            DeliveryStatus = NotificationDeliveryStatus.Failed;
        }

    }
}