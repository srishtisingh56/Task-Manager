using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MediatR;
using TaskManager.Application.Common.Exceptions;
using TaskManager.Application.Common.Interfaces;
using TaskManager.Application.Tasks.Common;
using TaskManager.Domain.Entities;
using TaskManager.Domain.Enums;

namespace TaskManager.Application.Tasks.Commands.UpdateTaskDetails
{
    public sealed class UpdateTaskDetailsCommandHandler(
        INotificationDispatcher notificationDispatcher,
        IApplicationDbContext db,
        ICurrentUserService currentUser)
        : IRequestHandler<UpdateTaskDetailsCommand, TaskDto>
    {
        public async Task<TaskDto> Handle(UpdateTaskDetailsCommand request, CancellationToken ct)
        {
            var task = await db.TaskItems.FindAsync([request.TaskId],ct)
            ?? throw new NotFoundException(nameof(TaskItem), request.TaskId);

            if(task.CreatedByUserId != currentUser.UserId)
            {
                throw new ForbiddenAccessException("Only the creator of the task can update its details.");
            }

           var title = request.Title.GetValueOrExisting(task.Title);
           var description = request.Description.GetValueOrExisting(task.Description);
           var priority = request.Priority.GetValueOrExisting(task.Priority);
            task.UpdateDetails(title,description,priority);

           await db.SaveChangesAsync(ct);

            if(task.AssignedToUserId != currentUser.UserId)
            {
                await notificationDispatcher.DispatchAsync(
                    taskId: task.Id,
                    recipientUserId: task.AssignedToUserId,
                    triggerEvent: NotificationTriggerEvent.Updated,
                    ct: ct
                );
            }

        return TaskDto.FromEntity(task);
        }
    }
}