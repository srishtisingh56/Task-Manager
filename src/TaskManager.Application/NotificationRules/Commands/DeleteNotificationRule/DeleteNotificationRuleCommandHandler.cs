using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MediatR;
using TaskManager.Application.Common.Exceptions;
using TaskManager.Application.Common.Interfaces;
using TaskManager.Domain.Entities;

namespace TaskManager.Application.NotificationRules.Commands.DeleteNotificationRule
{
    public class DeleteNotificationRuleCommandHandler(
        IApplicationDbContext db,
        ICurrentUserService currentUser
    ) : IRequestHandler<DeleteNotificationRuleCommand, Unit>
    {
        public async Task<Unit> Handle(DeleteNotificationRuleCommand request, CancellationToken ct)
        {
            var rule = await db.NotificationRules.FindAsync([request.RuleId], ct)
            ?? throw new NotFoundException(nameof(NotificationRule), request.RuleId);
            
            var task = await db.TaskItems.FindAsync([rule.TaskId], ct)
            ?? throw new NotFoundException(nameof(TaskItem), rule.TaskId);

            if (task.CreatedByUserId != currentUser.UserId)
            {
                throw new ForbiddenAccessException("Only the creator of the task can delete its notification rules.");
            }

            db.NotificationRules.Remove(rule);
            await db.SaveChangesAsync(ct);

            return Unit.Value;
        }
    }
}