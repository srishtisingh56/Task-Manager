
using MediatR;
using TaskManager.Application.Common.Exceptions;
using TaskManager.Application.Common.Interfaces;
using TaskManager.Application.Tasks.Common;
using TaskManager.Domain.Entities;
using TaskManager.Domain.Enums;

namespace TaskManager.Application.Tasks.Commands.MarkTaskCompleted
{
    public sealed class MarkTaskCompletedCommandHandler(
        INotificationDispatcher notificationDispatcher,
        IApplicationDbContext db,
        ICurrentUserService currentUser
    ) : IRequestHandler<MarkTaskCompletedCommand, TaskDto>
    {
        public async Task<TaskDto> Handle(MarkTaskCompletedCommand request, CancellationToken ct)
        {
            var task = await db.TaskItems.FindAsync([request.TaskId],ct)
            ?? throw new NotFoundException(nameof(TaskItem), request.TaskId);

            if(currentUser.UserId != task.AssignedToUserId && currentUser.UserId != task.CreatedByUserId)
            {
                throw new ForbiddenAccessException("Only worker or direct manager can complete the task.");
            }
            
            task.MarkCompleted();
            await db.SaveChangesAsync(ct);
            
            if (task.AssignedToUserId != task.CreatedByUserId)
            {
                await notificationDispatcher.DispatchAsync(
                    task.Id, task.CreatedByUserId, NotificationTriggerEvent.Completed, ct);
            }

            return TaskDto.FromEntity(task);

        }
    
    }
}