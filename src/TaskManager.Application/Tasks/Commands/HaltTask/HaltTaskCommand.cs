using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MediatR;
using TaskManager.Application.Tasks.Common;
namespace TaskManager.Application.Tasks.Commands.HaltTask
{
    public sealed record HaltTaskCommand
    (
        Guid TaskId
    ):IRequest<TaskDto>;
}