using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using TaskManager.Domain.Entities;
using TaskManager.Domain.Enums;

namespace TaskManager.Application.Tasks.Common
{
    public sealed record TaskDto(
     Guid Id,
     string Title,
     string? Description,
     TaskItemStatus Status,
     TaskPriority? Priority,
     DateTime LenientDeadline,
     DateTime StrictDeadline,
     bool IsRepetitive,
     Guid CreatedByUserId,
     Guid AssignedToUserId
    )
    {
        public static TaskDto FromEntity(TaskItem task) => new(
            task.Id,
            task.Title,
            task.Description,
            task.Status,
            task.Priority,
            task.LenientDeadline,
            task.StrictDeadline,
            task.IsRepetitive,
            task.CreatedByUserId,
            task.AssignedToUserId
        );
    }

}