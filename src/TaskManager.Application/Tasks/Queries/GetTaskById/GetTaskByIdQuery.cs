using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MediatR;
using TaskManager.Application.Tasks.Common;
namespace TaskManager.Application.Tasks.Queries.GetTaskById
{
    public sealed record GetTaskByIdQuery(
        Guid TaskId
    ) : IRequest<TaskDto>;
}