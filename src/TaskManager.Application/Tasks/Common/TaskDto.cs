using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
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
    );

}