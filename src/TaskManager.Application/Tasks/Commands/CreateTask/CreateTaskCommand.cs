using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MediatR;
using TaskManager.Application.Tasks.Common;
using TaskManager.Domain.Enums;
namespace TaskManager.Application.Tasks.Commands.CreateTask
{
    public sealed record CreateTaskCommand(
    string Title,
    string? Description,
    TaskPriority? Priority,
    DateTime LenientDeadline,
    DateTime StrictDeadline,
    bool IsRepetitive,
    Guid AssignedToUserId) : IRequest<TaskDto>;
}