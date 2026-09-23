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
            var user = await db.Users
                .AsNoTracking()
                .Where(u => u.Id == request.UserId)
                //UserDto.FromEntity can cause error in runtime -> convert to u=>new UserDto{..} format
                .Select(u => UserDto.FromEntity(u))
                .FirstOrDefaultAsync(ct)
                ?? throw new NotFoundException(nameof(User), request.UserId);

            // Self, your own direct manager, your own direct workers, or an
            // admin can view a profile.
            var isSelf = user.Id == currentUser.UserId;
            var isRelated = user.ManagerId == currentUser.UserId; // viewing your own direct worker

            if (!isSelf && !isRelated && !currentUser.IsSystemAdmin)
            {
                throw new ForbiddenAccessException("You do not have access to view this user.");
            }

            return user;
        }
    }
}