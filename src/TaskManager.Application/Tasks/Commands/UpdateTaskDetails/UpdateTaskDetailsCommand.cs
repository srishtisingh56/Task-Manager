using MediatR;
using TaskManager.Application.Common.Models;
using TaskManager.Application.Tasks.Common;
using TaskManager.Domain.Enums;

namespace TaskManager.Application.Tasks.Commands.UpdateTaskDetails
{
    public sealed record UpdateTaskDetailsCommand
    (
        Guid TaskId,
        Optional<string> Title,
        Optional<string?> Description,
        Optional<TaskPriority?> Priority
    ) : IRequest<TaskDto>;
}
    
    