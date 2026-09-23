
using MediatR;
using TaskManager.Application.Users.Common;
namespace TaskManager.Application.Users.Commands.AssignManager
{
    
    public sealed record AssignManagerCommand(
        Guid WorkerId,
        Guid ManagerId) : IRequest<UserDto>;
}