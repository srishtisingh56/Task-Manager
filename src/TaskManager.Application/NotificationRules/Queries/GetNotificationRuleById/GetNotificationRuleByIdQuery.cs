using System;
using MediatR;
using TaskManager.Application.NotificationRules.Common;

namespace TaskManager.Application.NotificationRules.Queries.GetNotificationRuleById
{
    public sealed record GetNotificationRuleByIdQuery(
        Guid RuleId
    ) : IRequest<NotificationRuleDto>;
}
