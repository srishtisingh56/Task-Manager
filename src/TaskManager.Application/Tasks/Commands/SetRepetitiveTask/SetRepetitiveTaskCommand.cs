
using TaskManager.Application.Tasks.Common;
using MediatR;

namespace TaskManager.Application.Tasks.Commands.SetRepetitiveTask
{
    public sealed record SetRepetitiveTaskCommand
    (
        Guid TaskId,
        bool IsRepetitive
    ): IRequest<TaskDto>;
}