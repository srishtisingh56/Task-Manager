using System;
using System.Collections.Generic;
using MediatR;
using TaskManager.Application.Common.Models;
using TaskManager.Application.NotificationRules.Common;

namespace TaskManager.Application.NotificationRules.Queries.GetNotificationRulesForTask
{
    public sealed record GetNotificationRulesForTaskQuery(
        Guid TaskId,
        int PageNumber = 1,
        int PageSize = 10
    ) : IRequest<PagedResult<NotificationRuleDto>>;
}
