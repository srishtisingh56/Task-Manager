using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MediatR;
using TaskManager.Application.Common.Exceptions;
using TaskManager.Application.Common.Interfaces;
using TaskManager.Application.Tasks.Common;
using TaskManager.Domain.Entities;

namespace TaskManager.Application.Tasks.Commands.UpdateTaskDetails
{
    public class UpdateTaskDetailsCommandHandler(
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

        return new TaskDto(
            task.Id, task.Title, task.Description, task.Status, task.Priority,
            task.LenientDeadline, task.StrictDeadline, task.IsRepetitive,
            task.CreatedByUserId, task.AssignedToUserId);
        }
    }
}