using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MediatR;
using Microsoft.EntityFrameworkCore;
using TaskManager.Application.Common.Exceptions;
using TaskManager.Application.Common.Interfaces;
using TaskManager.Application.Users.Common;
using TaskManager.Domain.Entities;

namespace TaskManager.Application.Users.Queries.GetUserById
{
    public sealed class GetUserByIdQueryHandler(
        IApplicationDbContext db,
        ICurrentUserService currentUser
    ) : IRequestHandler<GetUserByIdQuery, UserDto>
    {
        public async Task<UserDto> Handle(GetUserByIdQuery request, CancellationToken ct)
        {
            var targetUser = await db.Users
                .AsNoTracking()
                .Where(u => u.Id == request.UserId)
                .Select(u => new UserDto(u.Id, u.Name, u.Email, u.PhoneNumber, u.ManagerId, u.IsSystemAdmin))
                .FirstOrDefaultAsync(ct)
                ?? throw new NotFoundException(nameof(User), request.UserId);

            // Self, your own direct manager, your own direct workers, or an
            // admin can view a profile.
            var isSelf = targetUser.Id == currentUser.UserId;
            var isRelated = targetUser.ManagerId == currentUser.UserId; // viewing your own direct worker

            var self = await db.Users.FindAsync([currentUser.UserId], ct)
                ?? throw new NotFoundException(nameof(User), currentUser.UserId);

            var isMyManager = self.ManagerId == targetUser.Id;

            if (!currentUser.IsSystemAdmin && !isSelf && !isRelated && !isMyManager)
            {
                throw new ForbiddenAccessException("You do not have access to view this user.");
            }

            return targetUser;
        }
    }
}