using MediatR;
using TaskManager.Application.Common.Exceptions;
using TaskManager.Application.Common.Interfaces;
using TaskManager.Application.Users.Common;
using TaskManager.Domain.Entities;

namespace TaskManager.Application.Users.Commands.ReactivateUser;

public sealed class ReactivateUserCommandHandler(
    IApplicationDbContext db,
    ICurrentUserService currentUser
) : IRequestHandler<ReactivateUserCommand, UserDto>
{
    public async Task<UserDto> Handle(ReactivateUserCommand request, CancellationToken ct)
    {
        if (!currentUser.IsSystemAdmin)
        {
            throw new ForbiddenAccessException("Only a system admin can reactivate a user.");
        }

        var user = await db.Users.FindAsync([request.UserId], ct)
            ?? throw new NotFoundException(nameof(User), request.UserId);

        user.Reactivate();
        await db.SaveChangesAsync(ct);

        return UserDto.FromEntity(user);
    }
}