using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MediatR;
using Microsoft.EntityFrameworkCore;
using TaskManager.Application.Common.Interfaces;
using TaskManager.Application.Tasks.Common;

namespace TaskManager.Application.Tasks.Queries.GetTasksForUser
{
    public sealed class GetTasksForUserQueryHandler(
        IApplicationDbContext db,
        ICurrentUserService currentUser
    ) : IRequestHandler<GetTasksForUserQuery, PagedResult<TaskDto>>
    {
        public async Task<PagedResult<TaskDto>> Handle(GetTasksForUserQuery request, CancellationToken ct)
        {
            var query = db.TaskItems.AsNoTracking().Where(t=>!t.IsDeleted);
            if(!currentUser.IsSystemAdmin)
            {
                query = query.Where(t => t.CreatedByUserId == currentUser.UserId || t.AssignedToUserId == currentUser.UserId);
            }
            if(request.StatusFilter.HasValue)
            {
                query = query.Where(t => t.Status == request.StatusFilter.Value);
            }

            var totalCount = await query.CountAsync(ct);
            var items = await query
                .Skip((request.PageNumber - 1) * request.PageSize)
                .Take(request.PageSize)
                .Select(t => new TaskDto(
                    t.Id, t.Title, t.Description, t.Status, t.Priority,
                    t.LenientDeadline, t.StrictDeadline, t.IsRepetitive,
                    t.CreatedByUserId, t.AssignedToUserId))
                .ToListAsync(ct);

            return new PagedResult<TaskDto>(
                items,
                totalCount,
                request.PageNumber,
                request.PageSize
            );

        }
    }
}