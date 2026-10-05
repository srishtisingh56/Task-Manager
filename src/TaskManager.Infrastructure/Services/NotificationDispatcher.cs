using System;
using System.Net;
using System.Net.Mail;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using TaskManager.Application.Common.Interfaces;
using TaskManager.Application.Common.Notifications;
using TaskManager.Domain.Entities;
using TaskManager.Domain.Enums;

namespace TaskManager.Infrastructure.Services
{
    public class NotificationDispatcher : INotificationDispatcher
    {
        private readonly IApplicationDbContext _db;
        private readonly IDateTime _dateTime;
        private readonly EmailSettings _emailSettings;
        private readonly ILogger<NotificationDispatcher> _logger;

        public NotificationDispatcher(
            IApplicationDbContext db,
            IDateTime dateTime,
            IOptions<EmailSettings> emailSettings,
            ILogger<NotificationDispatcher> logger)
        {
            _db = db;
            _dateTime = dateTime;
            _emailSettings = emailSettings.Value;
            _logger = logger;
        }

        public async Task DispatchAsync(
            Guid taskId,
            Guid recipientUserId,
            NotificationTriggerEvent triggerEvent,
            CancellationToken ct,
            Guid? notificationRuleId = null)
        {
            var recipient = await _db.Users.AsNoTracking()
                .FirstOrDefaultAsync(u => u.Id == recipientUserId, ct);
            if (recipient is null || !recipient.IsActive)
                return; // recipient gone or deactivated; nothing to deliver

            var channels = ChannelPolicy.GetChannels(triggerEvent);

            foreach (var channel in channels)
            {
                var log = NotificationLog.RecordAttempt(notificationRuleId, taskId, triggerEvent, recipientUserId, channel, _dateTime.UtcNow);
                _db.NotificationLogs.Add(log);

                try
                {
                    await SendAsync(channel, recipient, taskId, triggerEvent, ct);
                    log.MarkSent();
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex,
                        "Failed to deliver {Channel} notification for task {TaskId} to user {UserId}",
                        channel, taskId, recipientUserId);
                    log.MarkFailed();
                }
            }
            try
            {
                await _db.SaveChangesAsync(ct);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,
                    "Failed to save notification logs for task {TaskId} to user {UserId}",
                    taskId, recipientUserId);
                throw;
            }
        }

        private Task SendAsync(
            NotificationChannel channel,
            User recipient,
            Guid taskId,
            NotificationTriggerEvent triggerEvent,
            CancellationToken ct)
        {
            return channel switch
            {
                // App notifications are represented by the NotificationLog row itself; no external transport needed.
                NotificationChannel.App => Task.CompletedTask,
                NotificationChannel.Email => SendEmailAsync(recipient.Email, taskId, triggerEvent, ct),
                NotificationChannel.Sms => throw new NotSupportedException("SMS delivery is not implemented yet."),
                _ => throw new ArgumentOutOfRangeException(nameof(channel), channel, null)
            };
        }

    private async Task SendEmailAsync(string toAddress, Guid taskId, NotificationTriggerEvent triggerEvent, CancellationToken ct)
    {
        var friendlyEvent = triggerEvent switch
        {
            NotificationTriggerEvent.Completed => "Task completed",
            NotificationTriggerEvent.StrictDeadlinePassed => "Task is now overdue",
            NotificationTriggerEvent.Created => "New task assigned to you",
            NotificationTriggerEvent.Updated => "Task updated",
            NotificationTriggerEvent.Deleted => "Task deleted",
            NotificationTriggerEvent.BeforeLenientDeadline => "Deadline reminder",
            NotificationTriggerEvent.BeforeStrictDeadline => "Deadline approaching soon",
            NotificationTriggerEvent.AfterCreationOffset => "Task reminder",
            _ => "Task update"
        };

        using var message = new MailMessage(_emailSettings.FromAddress, toAddress)
        {
            Subject = $"TaskFlow — {friendlyEvent}",
            Body = $"Hello,\n\n{friendlyEvent}.\n\nTask reference: {taskId}\n\nThis is an automated notification from TaskFlow."
        };

        using var client = new SmtpClient(_emailSettings.Host, _emailSettings.Port)
        {
            EnableSsl = _emailSettings.EnableSsl,
            Credentials = new NetworkCredential(_emailSettings.Username, _emailSettings.Password)
        };

        await client.SendMailAsync(message, ct);
        }
    }
}

