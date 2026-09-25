using System.Threading;
using System.Threading.Tasks;
using MediatR;
using Microsoft.EntityFrameworkCore;
using TaskManager.Application.Common.Exceptions;
using TaskManager.Application.Common.Interfaces;
using TaskManager.Application.NotificationRules.Common;
using TaskManager.Domain.Entities;

namespace TaskManager.Application.NotificationRules.Queries.GetNotificationRuleById
{
    public sealed class GetNotificationRuleByIdQueryHandler(
        IApplicationDbContext db,
        ICurrentUserService currentUser
    ) : IRequestHandler<GetNotificationRuleByIdQuery, NotificationRuleDto>
    {
        public async Task<NotificationRuleDto> Handle(GetNotificationRuleByIdQuery request, CancellationToken ct)
        {
            var rule = await db.NotificationRules.FindAsync([request.RuleId], ct)
                ?? throw new NotFoundException(nameof(NotificationRule), request.RuleId);

            var task = await db.TaskItems.FindAsync([rule.TaskId], ct)
                ?? throw new NotFoundException(nameof(TaskItem), rule.TaskId);

            // Notification rules are the creator's reminder configuration, not assignee-facing.
            if (task.CreatedByUserId != currentUser.UserId && !currentUser.IsSystemAdmin)
            {
                throw new ForbiddenAccessException("Only the creator of the task can view its notification rules.");
            }

            return NotificationRuleDto.FromEntity(rule);
        }
    }
}
