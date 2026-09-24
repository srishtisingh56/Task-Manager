
using MediatR;
using TaskManager.Application.Common.Models;
using TaskManager.Application.NotificationRules.Common;
using TaskManager.Domain.Enums;
namespace TaskManager.Application.NotificationRules.Commands.UpdateSchedule
{
    public sealed record UpdateScheduleCommand(
    Guid RuleId,
    Optional<NotificationTriggerEvent> TriggerEvent,
    Optional<int?> OffsetValue,
    Optional<NotificationOffsetUnit?> OffsetUnit,
    Optional<NotificationRepeatMode> RepeatMode
    ) : IRequest<NotificationRuleDto>;
}