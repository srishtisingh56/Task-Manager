
using MediatR;
using TaskManager.Application.Users.Common;
using TaskManager.Domain.Entities;

namespace TaskManager.Application.Users.Queries.GetUserById
{
   public sealed record GetUserByIdQuery(
    Guid UserId) : IRequest<UserDetailDto>;
}