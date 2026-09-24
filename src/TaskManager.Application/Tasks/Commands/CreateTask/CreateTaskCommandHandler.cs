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
using TaskManager.Domain.Enums;

namespace TaskManager.Application.Tasks.Commands.CreateTask
{
    public sealed class CreateTaskCommandHandler(
        INotificationDispatcher notificationDispatcher,
        IApplicationDbContext db,
        ICurrentUserService currentUser)
        : IRequestHandler<CreateTaskCommand, TaskDto>
    {
        public async Task<TaskDto> Handle(CreateTaskCommand request, CancellationToken ct)
        {
            var user = await db.Users.FindAsync([currentUser.UserId],ct)
                ?? throw new NotFoundException(nameof(User), currentUser.UserId);

            if(request.AssignedToUserId != currentUser.UserId)
            {
                var assignee = await db.Users.FindAsync([request.AssignedToUserId], ct)
                    ?? throw new NotFoundException(nameof(User), request.AssignedToUserId);

                if(!user.IsDirectManagerOf(assignee))
                {
                    throw new ForbiddenAccessException("You can only assign tasks to yourself or your direct reports.");
                }
            }

            var task = TaskItem.Create(
                title:request.Title,
                description:request.Description,
                assignedToUserId:request.AssignedToUserId,
                createdByUserId:currentUser.UserId,
                lenientDeadline:request.LenientDeadline,
                strictDeadline:request.StrictDeadline,
                isRepetitive:request.IsRepetitive,
                priority:request.Priority
            );

            db.TaskItems.Add(task);
            await db.SaveChangesAsync(ct);
            
            if(task.AssignedToUserId != currentUser.UserId)
            {
                await notificationDispatcher.DispatchAsync(
                    taskId: task.Id,
                    recipientUserId: task.AssignedToUserId,
                    triggerEvent: NotificationTriggerEvent.Created,
                    ct: ct
                );
            }
            return TaskDto.FromEntity(task);
        }
    }
}