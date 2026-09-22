using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MediatR;
namespace TaskManager.Application.Tasks.Commands.DeleteTaskCommand
{
    public sealed record DeleteTaskCommand
    (
        Guid TaskId
    ):IRequest<Unit>;
}