using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore.Infrastructure;
using MediatR;
using TaskManager.Application.Common.Interfaces;
using TaskManager.Application.Common.Exceptions;
using TaskManager.Domain.Entities;
using TaskManager.Application.Tasks.Common;
using TaskManager.Domain.Enums;

namespace TaskManager.Application.Tasks.Commands.SetRepetitiveTask
{
    public sealed class SetRepetitiveTaskCommandHandler(
        INotificationDispatcher notificationDispatcher,
        IApplicationDbContext db,
        ICurrentUserService currentUser
    ) : IRequestHandler<SetRepetitiveTaskCommand, TaskDto>
    {
        public async Task<TaskDto> Handle(SetRepetitiveTaskCommand request, CancellationToken ct)
        {
            var task = await db.TaskItems.FindAsync([request.TaskId], ct)
                ?? throw new NotFoundException(nameof(TaskItem), request.TaskId);

            if(task.CreatedByUserId != currentUser.UserId)
            {
                throw new ForbiddenAccessException("Only the creator of the task can set it as repetitive");
            }

            task.SetRepetitive(request.IsRepetitive);
            await db.SaveChangesAsync(ct);

            await notificationDispatcher.DispatchAsync(
                taskId: task.Id,
                recipientUserId: task.AssignedToUserId,
                triggerEvent: NotificationTriggerEvent.Updated,
                ct: ct
            );

            return TaskDto.FromEntity(task);
        }
    }
}