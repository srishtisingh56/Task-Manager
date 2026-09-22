using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using TaskManager.Domain.Enums;
using MediatR;

namespace TaskManager.Application.Tasks.Commands.DeleteTasksByStatus
{
    public sealed record  DeleteTasksByStatusCommand
    (
        TaskItemStatus Status
    ):IRequest<int>;
}