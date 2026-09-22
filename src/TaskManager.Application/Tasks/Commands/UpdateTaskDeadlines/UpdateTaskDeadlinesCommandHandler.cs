
using MediatR;
using TaskManager.Application.Common.Exceptions;
using TaskManager.Application.Common.Interfaces;
using TaskManager.Application.Tasks.Common;
using TaskManager.Domain.Entities;

namespace TaskManager.Application.Tasks.Commands.UpdateTaskDeadlines
{
    public sealed class UpdateTaskDeadlinesCommandHandler(
        IApplicationDbContext db,
        ICurrentUserService currentUser
    ):IRequestHandler<UpdateTaskDeadlinesCommand, TaskDto>
    {
        public async Task<TaskDto> Handle(UpdateTaskDeadlinesCommand request, CancellationToken ct)
        {
            var task = await db.TaskItems.FindAsync([request.TaskId],ct)
            ?? throw new NotFoundException(nameof(TaskItem), request.TaskId);

            if(task.CreatedByUserId != currentUser.UserId)
            {
                throw new ForbiddenAccessException("Only the creator of the task can update its deadlines.");
            }

            var lenientDeadline = request.LenientDeadline.GetValueOrExisting(task.LenientDeadline);
            var strictDeadline = request.StrictDeadline.GetValueOrExisting(task.StrictDeadline);
            task.UpdateDeadlines(lenientDeadline, strictDeadline);

            await db.SaveChangesAsync(ct);

            return TaskDto.FromEntity(task);
        }
    }
}