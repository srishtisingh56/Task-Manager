using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MediatR;
using TaskManager.Application.Common.Exceptions;
using TaskManager.Application.Common.Interfaces;
using TaskManager.Domain.Entities;
using TaskManager.Domain.Enums;

namespace TaskManager.Application.Tasks.Commands.DeleteTaskCommand
{
    public sealed class DeleteTaskCommandHandler(
        INotificationDispatcher notificationDispatcher,
        IApplicationDbContext db,
        ICurrentUserService currentUser
    )
    : IRequestHandler<DeleteTaskCommand, Unit>
    {
        public async Task<Unit> Handle(DeleteTaskCommand request, CancellationToken ct)
        {
            var task = await db.TaskItems.FindAsync([request.TaskId], ct)
                ?? throw new NotFoundException(nameof(TaskItem), request.TaskId);

            if(task.CreatedByUserId != currentUser.UserId)
            {
                throw new ForbiddenAccessException("Only the creator of the task can delete the task");
            }

            task.Delete();
            await db.SaveChangesAsync(ct);
            
        
            await notificationDispatcher.DispatchAsync(
                taskId: task.Id,
                recipientUserId: task.AssignedToUserId,
                triggerEvent: NotificationTriggerEvent.Deleted,
                ct: ct
            );
        
            return Unit.Value;
        }
    }
}