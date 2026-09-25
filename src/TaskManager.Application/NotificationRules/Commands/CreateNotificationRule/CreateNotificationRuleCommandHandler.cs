using MediatR;
using TaskManager.Application.Common.Exceptions;
using TaskManager.Application.Common.Interfaces;
using TaskManager.Application.NotificationRules.Common;

namespace TaskManager.Application.NotificationRules.Commands.CreateNotificationRule
    {
        public sealed class CreateNotificationRuleCommandHandler(
        IApplicationDbContext db,
        ICurrentUserService currentUser
    ) : IRequestHandler<CreateNotificationRuleCommand, NotificationRuleDto>
    {
        public async Task<NotificationRuleDto> Handle(CreateNotificationRuleCommand request, CancellationToken ct)
        {
            var task = await db.TaskItems.FindAsync([request.TaskId], ct)
                ?? throw new NotFoundException(nameof(Domain.Entities.TaskItem), request.TaskId);

            if (task.CreatedByUserId != currentUser.UserId)
            {
                throw new ForbiddenAccessException("Only the creator of the task can configure its notification rules.");
            }

            var rule = Domain.Entities.NotificationRule.Create(
                taskId: request.TaskId,
                triggerEvent: request.TriggerEvent,
                offsetValue: request.OffsetValue,
                offsetUnit: request.OffsetUnit,
                repeatMode: request.RepeatMode);

            db.NotificationRules.Add(rule);
            await db.SaveChangesAsync(ct);

            return NotificationRuleDto.FromEntity(rule);
        }
    }
}