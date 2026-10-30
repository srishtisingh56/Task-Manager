using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MediatR;
using TaskManager.Application.Common.Models;
using TaskManager.Application.NotificationLogs.Common;
using TaskManager.Domain.Enums;

namespace TaskManager.Application.NotificationLogs.Queries.GetNotificationLogs
{
   public sealed record GetNotificationLogsQuery(
    Guid? TaskId = null,
    NotificationChannel? Channel = null,
    bool OnlyMine = false,
    int PageNumber = 1,
    int PageSize = 20)
    : IRequest<PagedResult<NotificationLogDto>>;
}