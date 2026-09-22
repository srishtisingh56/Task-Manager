using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MediatR;
using TaskManager.Application.Common.Exceptions;
using TaskManager.Application.Common.Interfaces;
using TaskManager.Domain.Entities;

namespace TaskManager.Application.Tasks.Commands.DeleteTaskCommand
{
    public sealed class DeleteTaskCommandHandler(
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

            return Unit.Value;
        }
    }
}