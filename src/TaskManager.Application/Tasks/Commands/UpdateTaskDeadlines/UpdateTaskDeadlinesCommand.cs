using MediatR;
using TaskManager.Application.Common.Models;
using TaskManager.Application.Tasks.Common;
namespace TaskManager.Application.Tasks.Commands.UpdateTaskDeadlines
{
    public sealed record UpdateTaskDeadlinesCommand
    (
        Guid TaskId,
        Optional<DateTime> LenientDeadline,
        Optional<DateTime> StrictDeadline
    ): IRequest<TaskDto>;
}