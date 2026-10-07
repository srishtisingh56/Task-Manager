
using MediatR;
using Microsoft.EntityFrameworkCore;
using TaskManager.Application.Common.Exceptions;
using TaskManager.Application.Common.Interfaces;
using TaskManager.Application.Tasks.Common;
using TaskManager.Domain.Entities;
using TaskManager.Domain.Enums;

namespace TaskManager.Application.Tasks.Commands.UpdateTaskDeadlines
{
    public sealed class UpdateTaskDeadlinesCommandHandler(
        INotificationDispatcher notificationDispatcher,
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

            var hasStaleOnceRules = await db.NotificationRules
            .Where(r => r.TaskId == task.Id
                && r.RepeatMode == NotificationRepeatMode.Once
                && (r.TriggerEvent == NotificationTriggerEvent.BeforeLenientDeadline
                    || r.TriggerEvent == NotificationTriggerEvent.BeforeStrictDeadline))
            .AnyAsync(r => db.NotificationLogs.Any(l => l.NotificationRuleId == r.Id), ct);

            var lenientDeadline = request.LenientDeadline.GetValueOrExisting(task.LenientDeadline);
            var strictDeadline = request.StrictDeadline.GetValueOrExisting(task.StrictDeadline);
            task.UpdateDeadlines(lenientDeadline, strictDeadline);

            await db.SaveChangesAsync(ct);

            await notificationDispatcher.DispatchAsync(
                taskId: task.Id,
                recipientUserId: task.AssignedToUserId,
                triggerEvent: NotificationTriggerEvent.Updated,
                ct: ct
            );

            var dto = TaskDto.FromEntity(task);
            return hasStaleOnceRules
                ? dto with { Warning = "Deadlines changed. Existing 'before deadline' reminders already sent won't resend — delete and re-add them if you need a new reminder for the updated time." }
                : dto;
                }
    }
}