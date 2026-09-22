
using MediatR;
using TaskManager.Application.Tasks.Common;
namespace TaskManager.Application.Tasks.Commands.RestoreTask
{
    public sealed record RestoreTaskCommand
    (
        Guid TaskId
    ):IRequest<TaskDto>;
}