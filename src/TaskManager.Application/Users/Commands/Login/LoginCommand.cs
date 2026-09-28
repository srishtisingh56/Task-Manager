using MediatR;
using TaskManager.Application.Users.Common;

namespace TaskManager.Application.Users.Commands.Login
{
    public sealed record LoginCommand(
        string Email,
        string Password
    ) : IRequest<AuthResultDto>;
}
