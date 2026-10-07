using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MediatR;
using TaskManager.Application.Common.Models;
using TaskManager.Application.NotificationLogs.Common;

namespace TaskManager.Application.NotificationLogs.Queries.GetNotificationLogs
{
    public sealed record GetNotificationLogsQuery(
        Guid? TaskId = null, 
        int PageNumber = 1, 
        int PageSize = 20)
    : IRequest<PagedResult<NotificationLogDto>>;
}