
using MediatR;
using TaskManager.Application.NotificationRules.Common;
using TaskManager.Domain.Enums;

namespace TaskManager.Application.NotificationRules.Commands.CreateNotificationRule
{
    public sealed record CreateNotificationRuleCommand(
    Guid TaskId,
    NotificationTriggerEvent TriggerEvent,
    int? OffsetValue,
    NotificationOffsetUnit? OffsetUnit,
    NotificationRepeatMode RepeatMode,
    NotificationChannel Channel) : IRequest<NotificationRuleDto>;
}