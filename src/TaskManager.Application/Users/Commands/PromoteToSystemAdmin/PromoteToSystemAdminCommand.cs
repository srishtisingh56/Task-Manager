using MediatR;
using TaskManager.Application.Users.Common;
namespace TaskManager.Application.Users.Commands.PromoteToSystemAdmin
{
    public sealed record PromoteToSystemAdminCommand(
        Guid UserId
    ) : IRequest<UserDto>;
}