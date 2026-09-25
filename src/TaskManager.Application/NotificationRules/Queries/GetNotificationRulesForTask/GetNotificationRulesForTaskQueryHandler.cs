using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using Microsoft.EntityFrameworkCore;
using TaskManager.Application.Common.Exceptions;
using TaskManager.Application.Common.Interfaces;
using TaskManager.Application.Common.Models;
using TaskManager.Application.NotificationRules.Common;
using TaskManager.Domain.Entities;

namespace TaskManager.Application.NotificationRules.Queries.GetNotificationRulesForTask
{
    public sealed class GetNotificationRulesForTaskQueryHandler(
        IApplicationDbContext db,
        ICurrentUserService currentUser
    ) : IRequestHandler<GetNotificationRulesForTaskQuery, PagedResult<NotificationRuleDto>>
    {
        public async Task<PagedResult<NotificationRuleDto>> Handle(GetNotificationRulesForTaskQuery request, CancellationToken ct)
        {
            var task = await db.TaskItems.FindAsync([request.TaskId], ct)
                ?? throw new NotFoundException(nameof(TaskItem), request.TaskId);

            // Notification rules are the creator's reminder configuration, not assignee-facing.
            if (task.CreatedByUserId != currentUser.UserId && !currentUser.IsSystemAdmin)
            {
                throw new ForbiddenAccessException("Only the creator of the task can view its notification rules.");
            }

            var query = db.NotificationRules.AsNoTracking().Where(r => r.TaskId == request.TaskId);

            var totalCount = await query.CountAsync(ct);
            var items = await query
                .Skip((request.PageNumber - 1) * request.PageSize)
                .Take(request.PageSize)
                .Select(r => new NotificationRuleDto(
                    r.Id, r.TaskId, r.TriggerEvent, r.OffsetValue, r.OffsetUnit, r.RepeatMode))
                .ToListAsync(ct);

            return new PagedResult<NotificationRuleDto>(
                items,
                totalCount,
                request.PageNumber,
                request.PageSize
            );
        }
    }
}
