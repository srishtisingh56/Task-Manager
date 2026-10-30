using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MediatR;
using Microsoft.EntityFrameworkCore;
using TaskManager.Application.Common.Interfaces;
using TaskManager.Application.Common.Models;
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
            var query = db.TaskItems.AsNoTracking().Where(
                t => !t.IsDeleted
            );
            var me = currentUser.UserId;

            if(!currentUser.IsSystemAdmin)
            {
                query = query.Where(t => t.CreatedByUserId == me || t.AssignedToUserId == me);
            }
            if(request.Status.HasValue)
            {
                query = query.Where(t => t.Status == request.Status.Value);
            }

            if(request.Relation == TaskRelationFilter.AssignedToMe)
            {
                query = query.Where(t => t.AssignedToUserId == me);
            }
            else if(request.Relation == TaskRelationFilter.AssignedByMe)
            {
                query = query.Where(t => t.CreatedByUserId == me && t.AssignedToUserId != me);
            }
            else if(request.Relation == TaskRelationFilter.Personal)
            {
                query = query.Where(t => t.CreatedByUserId == me && t.AssignedToUserId == me);
            }

            var totalCount = await query.CountAsync(ct);
            var items = await query
                .OrderByDescending(t=>t.CreatedAt)
                .ThenByDescending(t=>t.Id)
                .Skip((request.PageNumber - 1) * request.PageSize)
                .Take(request.PageSize)
                .Select(t => new TaskDto(
                    t.Id, t.Title, t.Description, t.Status, t.Priority,
                    t.LenientDeadline, t.StrictDeadline, t.IsRepetitive,
                    t.CreatedByUserId, t.AssignedToUserId,
                    db.Users.Where(u=>u.Id == t.CreatedByUserId).Select(u=>u.Name).FirstOrDefault(),
                    db.Users.Where(u=>u.Id == t.AssignedToUserId).Select(u=>u.Name).FirstOrDefault()
                    ))
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