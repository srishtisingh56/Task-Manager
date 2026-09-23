
using TaskManager.Domain.Enums;
using MediatR;
using TaskManager.Application.Tasks.Common;
using TaskManager.Application.Common.Models;

namespace TaskManager.Application.Tasks.Queries.GetTasksForUser
{
    public sealed record GetTasksForUserQuery(
        Guid? TargetUserId,
        TaskItemStatus? Status,
        int PageNumber = 1,
        int PageSize = 10
    ) : IRequest<PagedResult<TaskDto>>;

}