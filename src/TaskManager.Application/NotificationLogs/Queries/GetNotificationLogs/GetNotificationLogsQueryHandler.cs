
using MediatR;
using Microsoft.EntityFrameworkCore;
using TaskManager.Application.Common.Interfaces;
using TaskManager.Application.Common.Models;
using TaskManager.Application.NotificationLogs.Common;

namespace TaskManager.Application.NotificationLogs.Queries.GetNotificationLogs
{
    public sealed class GetNotificationLogsQueryHandler(IApplicationDbContext db, ICurrentUserService currentUser)
    : IRequestHandler<GetNotificationLogsQuery, PagedResult<NotificationLogDto>>
    {
        public async Task<PagedResult<NotificationLogDto>> Handle(GetNotificationLogsQuery request, CancellationToken ct)
        {
            var query = from l in db.NotificationLogs.AsNoTracking()
                        join t in db.TaskItems on l.TaskId equals t.Id
                        // Deliberately NOT filtering t.IsDeleted — audit trail should survive task deletion, per domain_layer.md 
                        where currentUser.IsSystemAdmin
                            || t.CreatedByUserId == currentUser.UserId
                            || l.RecipientUserId == currentUser.UserId
                        select new { l, t.Title };

            if (request.TaskId.HasValue)
                query = query.Where(x => x.l.TaskId == request.TaskId.Value);

            var ordered = query.OrderByDescending(x => x.l.SentAt);
            var total = await ordered.CountAsync(ct);
            var items = await ordered.Skip((request.PageNumber - 1) * request.PageSize).Take(request.PageSize)
                .Select(x => new NotificationLogDto(x.l.Id, x.l.TaskId, x.Title,
                    x.l.TriggerEvent, x.l.Channel, x.l.DeliveryStatus, x.l.SentAt))
                .ToListAsync(ct);

            return new PagedResult<NotificationLogDto>(items, total, request.PageNumber, request.PageSize);
        }
    }
}