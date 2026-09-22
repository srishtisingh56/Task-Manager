using MediatR;
using TaskManager.Application.Common.Exceptions;
using TaskManager.Application.Common.Interfaces;
using TaskManager.Application.Tasks.Common;
using TaskManager.Domain.Entities;
namespace TaskManager.Application.Tasks.Commands.RestoreTask
{
    public sealed class RestoreTaskCommandHandler( 
        IApplicationDbContext db,
        ICurrentUserService currentUser
    )
    : IRequestHandler<RestoreTaskCommand, TaskDto>
    {
        public async Task<TaskDto> Handle(RestoreTaskCommand request, CancellationToken ct)
        {
            var task = await db.TaskItems.FindAsync([request.TaskId], ct)
                ?? throw new NotFoundException(nameof(TaskItem), request.TaskId);

            if(task.CreatedByUserId != currentUser.UserId)
            {
                throw new ForbiddenAccessException("Only the creator of the task can restore the task");
            }

            task.Restore();
            await db.SaveChangesAsync(ct);

            return TaskDto.FromEntity(task);
        }
    }
}