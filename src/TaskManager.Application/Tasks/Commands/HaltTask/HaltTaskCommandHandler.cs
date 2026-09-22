using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MediatR;
using TaskManager.Application.Tasks.Common;
using System.Threading;
using TaskManager.Application.Common.Interfaces;
using TaskManager.Application.Common.Exceptions;
using TaskManager.Domain.Entities;
namespace TaskManager.Application.Tasks.Commands.HaltTask
{
    public sealed class HaltTaskCommandHandler(
        IApplicationDbContext db,
        ICurrentUserService currentUser
    )
    : IRequestHandler<HaltTaskCommand, TaskDto>
    {
        public async Task<TaskDto> Handle(HaltTaskCommand request, CancellationToken ct)
        {
            var task = await db.TaskItems.FindAsync([request.TaskId],ct)
            ?? throw new NotFoundException(nameof(TaskItem), request.TaskId);

            if(task.CreatedByUserId != currentUser.UserId)
            {
                throw new ForbiddenAccessException("Only the creator of the task can halt the task");
            }
            task.Halt();
            await db.SaveChangesAsync(ct);

            return TaskDto.FromEntity(task);
        }
    }
}