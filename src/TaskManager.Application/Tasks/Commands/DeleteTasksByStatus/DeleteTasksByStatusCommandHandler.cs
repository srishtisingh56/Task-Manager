using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MediatR;
using Microsoft.EntityFrameworkCore;
using TaskManager.Application.Common.Interfaces;
using TaskManager.Domain.Enums;
namespace TaskManager.Application.Tasks.Commands.DeleteTasksByStatus
{
    public sealed class DeleteTasksByStatusCommandHandler(
        INotificationDispatcher notificationDispatcher,
        IApplicationDbContext db,
        ICurrentUserService currentUser
        
    )
    : IRequestHandler<DeleteTasksByStatusCommand, int>
    {
        public async Task<int> Handle(DeleteTasksByStatusCommand request, CancellationToken ct)
        {
            var tasksToDelete = await db.TaskItems
                .Where(t => t.Status == request.Status
                        && t.CreatedByUserId == currentUser.UserId
                        && !t.IsDeleted)
                .ToListAsync(ct);

            foreach(var task in tasksToDelete)
            {
                task.Delete();
            }
            
            await db.SaveChangesAsync(ct);
            
            foreach(var task in tasksToDelete)
            {
                await notificationDispatcher.DispatchAsync(
                    taskId: task.Id,
                    recipientUserId: task.AssignedToUserId,
                    triggerEvent: NotificationTriggerEvent.Deleted,
                    ct: ct
                );
            }
            return tasksToDelete.Count;  
        }
    }
}