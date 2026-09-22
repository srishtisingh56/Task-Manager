using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MediatR;
using Microsoft.EntityFrameworkCore;
using TaskManager.Application.Common.Exceptions;    
using TaskManager.Application.Common.Interfaces;
using TaskManager.Application.Tasks.Common;
using TaskManager.Domain.Entities;
namespace TaskManager.Application.Tasks.Queries.GetTaskById
{
    public sealed class GetTaskByIdQueryHandler
    (
        IApplicationDbContext db,
        ICurrentUserService currentUser
    )
    : IRequestHandler<GetTaskByIdQuery, TaskDto>
    {
        public async Task<TaskDto> Handle(GetTaskByIdQuery request, CancellationToken ct)
        {
            var task = await db.TaskItems
                .Where(t => t.Id == request.TaskId)
                .Select(t => new TaskDto(
                    t.Id, t.Title, t.Description, t.Status, t.Priority,
                    t.LenientDeadline, t.StrictDeadline, t.IsRepetitive,
                    t.CreatedByUserId, t.AssignedToUserId))
                .FirstOrDefaultAsync(ct)
                ?? throw new NotFoundException(nameof(TaskItem), request.TaskId);

            if(task.AssignedToUserId != currentUser.UserId && task.CreatedByUserId != currentUser.UserId && !currentUser.IsSystemAdmin)
            {
                throw new ForbiddenAccessException("Only the creator or the assignee of the task can view this task.");
            }
            return task;
        }
    }
}