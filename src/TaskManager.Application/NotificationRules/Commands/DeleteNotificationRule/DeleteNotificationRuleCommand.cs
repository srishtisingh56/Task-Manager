using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MediatR;

namespace TaskManager.Application.NotificationRules.Commands.DeleteNotificationRule
{
    public sealed record DeleteNotificationRuleCommand
    (
        Guid RuleId
    ) : IRequest<Unit>;
}