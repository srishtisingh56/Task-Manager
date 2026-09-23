using MediatR;
using TaskManager.Application.Common.Interfaces;
using TaskManager.Application.Common.Exceptions;
using TaskManager.Application.Users.Common;
using TaskManager.Domain.Entities;
using TaskManager.Application.Users.Commands.PromoteToSystemAdmin;

namespace TaskManager.Application.Users.Commands.DemoteFromSystemAdmin
{
   public sealed class DemoteFromSystemAdminCommandHandler(
        IApplicationDbContext db,
        ICurrentUserService currentUser
    ) : IRequestHandler<DemoteFromSystemAdminCommand, UserDto>
    {
        public async Task<UserDto> Handle(DemoteFromSystemAdminCommand request, CancellationToken ct)
        {
            if (!currentUser.IsSystemAdmin)
            {
                throw new ForbiddenAccessException("Only an existing system admin can demote another user.");
            }

            var user = await db.Users.FindAsync([request.UserId], ct)
                ?? throw new NotFoundException(nameof(User), request.UserId);

            user.DemoteFromSystemAdmin();

            await db.SaveChangesAsync(ct);

            return UserDto.FromEntity(user);
        }
    }
}