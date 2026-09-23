using MediatR;
using TaskManager.Application.Common.Interfaces;
using TaskManager.Application.Common.Exceptions;
using TaskManager.Application.Users.Common;
using TaskManager.Domain.Entities;

namespace TaskManager.Application.Users.Commands.PromoteToSystemAdmin
{
    public sealed class PromoteToSystemAdminCommandHandler(
        IApplicationDbContext db,
        ICurrentUserService currentUser
    ) : IRequestHandler<PromoteToSystemAdminCommand, UserDto>
    {
        public async Task<UserDto> Handle(PromoteToSystemAdminCommand request, CancellationToken ct)
        {
            if (!currentUser.IsSystemAdmin)
            {
                throw new ForbiddenAccessException("Only an existing system admin can promote another user.");
            }

            var user = await db.Users.FindAsync([request.UserId], ct)
                ?? throw new NotFoundException(nameof(User), request.UserId);

            user.PromoteToSystemAdmin();

            await db.SaveChangesAsync(ct);

            return UserDto.FromEntity(user);
        }
    }
}