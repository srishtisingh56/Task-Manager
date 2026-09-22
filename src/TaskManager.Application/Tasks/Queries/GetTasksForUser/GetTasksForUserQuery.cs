using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using TaskManager.Domain.Enums;
using MediatR;
using TaskManager.Application.Tasks.Common;

namespace TaskManager.Application.Tasks.Queries.GetTasksForUser
{
    public sealed record GetTasksForUserQuery(
        TaskItemStatus? Status,
        int PageNumber = 1,
        int PageSize = 10
    ) : IRequest<PagedResult<TaskDto>>;

    public sealed record PagedResult<T>(
        IReadOnlyList<T> Items,
        int TotalCount,
        int PageNumber,
        int PageSize
    );
}