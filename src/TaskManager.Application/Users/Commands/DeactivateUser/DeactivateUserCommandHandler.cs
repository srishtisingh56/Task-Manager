using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MediatR;
using TaskManager.Application.Common.Exceptions;
using TaskManager.Application.Common.Interfaces;
using TaskManager.Application.Users.Common;
using TaskManager.Domain.Entities;

namespace TaskManager.Application.Users.Commands.DeactivateUser
{
   public sealed class DeactivateUserCommandHandler(
    IApplicationDbContext db,
    ICurrentUserService currentUser
) : IRequestHandler<DeactivateUserCommand, UserDto>
{
    public async Task<UserDto> Handle(DeactivateUserCommand request, CancellationToken ct)
    {
        if (!currentUser.IsSystemAdmin)
        {
            throw new ForbiddenAccessException("Only a system admin can deactivate a user.");
        }

        var user = await db.Users.FindAsync([request.UserId], ct)
            ?? throw new NotFoundException(nameof(User), request.UserId);

        // Prevent an admin from deactivating themselves and locking
        // themselves out
        if (user.Id == currentUser.UserId)
        {
            throw new ForbiddenAccessException("You cannot deactivate your own account.");
        }

        user.Deactivate();
        await db.SaveChangesAsync(ct);

        return UserDto.FromEntity(user);
    }
}
}