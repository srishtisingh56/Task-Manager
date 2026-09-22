using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MediatR;
using TaskManager.Application.Common.Exceptions;
using TaskManager.Application.Common.Interfaces;
using TaskManager.Application.Tasks.Common;
using TaskManager.Domain.Entities;

namespace TaskManager.Application.Tasks.Commands.ReassignTask
{
    public class ReassignTaskCommandHandler(
        IApplicationDbContext db,
        ICurrentUserService currentUser
    ) : IRequestHandler<ReassignTaskCommand, TaskDto>
    {
        public async Task<TaskDto> Handle(ReassignTaskCommand request, CancellationToken ct)
        {
            var task = await db.TaskItems.FindAsync([request.TaskId], ct)
            ?? throw new NotFoundException(nameof(TaskItem), request.TaskId);

            if (task.CreatedByUserId != currentUser.UserId)
            {
                throw new ForbiddenAccessException("Only the creator of the task can reassign the task");
            }

            if (request.NewAssigneeId != currentUser.UserId)
            {
                var assigner = await db.Users.FindAsync([currentUser.UserId], ct)
                ?? throw new NotFoundException(nameof(User), currentUser.UserId);

                var newAssignee = await db.Users.FindAsync([request.NewAssigneeId], ct)
                ?? throw new NotFoundException(nameof(User), request.NewAssigneeId);

                if (!assigner.IsDirectManagerOf(newAssignee))
                {
                    throw new ForbiddenAccessException("Only a direct manager can reassign the task to this user");
                }
            }
            task.Reassign(request.NewAssigneeId);           
            await db.SaveChangesAsync(ct);

            return new TaskDto(
               task.Id, task.Title, task.Description, task.Status, task.Priority,
               task.LenientDeadline, task.StrictDeadline, task.IsRepetitive,
               task.CreatedByUserId, task.AssignedToUserId
           );
        }
    }
}