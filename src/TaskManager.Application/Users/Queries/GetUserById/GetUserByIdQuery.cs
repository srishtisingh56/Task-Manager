
using MediatR;
using TaskManager.Application.Users.Common;

namespace TaskManager.Application.Users.Queries.GetUserById
{
   public sealed record GetUserByIdQuery(
    Guid UserId) : IRequest<UserDto>;
}