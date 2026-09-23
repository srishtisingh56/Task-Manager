using MediatR;
using TaskManager.Application.Users.Common;
namespace TaskManager.Application.Users.Commands.DemoteFromSystemAdmin
{
    public sealed record DemoteFromSystemAdminCommand(
        Guid UserId
    ) : IRequest<UserDto>;
}