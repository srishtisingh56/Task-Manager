using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MediatR;
using TaskManager.Application.Common.Exceptions;
using TaskManager.Application.Common.Interfaces;
using TaskManager.Application.NotificationRules.Common;

namespace TaskManager.Application.NotificationRules.Commands.UpdateSchedule
{
     public sealed class UpdateScheduleCommandHandler(
        IApplicationDbContext db,
        ICurrentUserService currentUser
    ) : IRequestHandler<UpdateScheduleCommand, NotificationRuleDto>
    {
        public async Task<NotificationRuleDto> Handle(UpdateScheduleCommand request, CancellationToken ct)
        {
            var rule = await db.NotificationRules.FindAsync([request.RuleId], ct)
                ?? throw new NotFoundException(nameof(Domain.Entities.TaskItem), request.RuleId);

            var task = await db.TaskItems.FindAsync([rule.TaskId], ct)
                ?? throw new NotFoundException(nameof(Domain.Entities.TaskItem), rule.TaskId);
            
            if (task.CreatedByUserId != currentUser.UserId)
            {
                throw new ForbiddenAccessException("Only the creator of the task can update its notification rules.");
            }

            var triggerEvent = request.TriggerEvent.GetValueOrExisting(rule.TriggerEvent);
            var offsetValue = request.OffsetValue.GetValueOrExisting(rule.OffsetValue);
            var offsetUnit = request.OffsetUnit.GetValueOrExisting(rule.OffsetUnit);
            var repeatMode = request.RepeatMode.GetValueOrExisting(rule.RepeatMode);

           rule.UpdateSchedule(
              triggerEvent,
              offsetUnit,
              offsetValue,
              repeatMode
              
           );
            await db.SaveChangesAsync(ct);

            return NotificationRuleDto.FromEntity(rule);
        }
    }
}