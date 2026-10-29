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
    ) : IRequestHandler<GetUserByIdQuery, UserDetailDto>
    {
        public async Task<UserDetailDto> Handle(GetUserByIdQuery request, CancellationToken ct)
        {
            var target = await db.Users
                .AsNoTracking()
                .Where(u => u.Id == request.UserId)
                .Select(u => new UserDetailDto(
                    u.Id, u.Name, u.Email, u.PhoneNumber,
                    u.ManagerId,
                    db.Users.Where(m => m.Id == u.ManagerId).Select(m => m.Name).FirstOrDefault(),
                    u.IsSystemAdmin, u.IsActive))
                .FirstOrDefaultAsync(ct)
                ?? throw new NotFoundException(nameof(User), request.UserId);

            var me = currentUser.UserId;
            var myManagerId = await db.Users
                .Where(u => u.Id == me)
                .Select(u => u.ManagerId)
                .FirstOrDefaultAsync(ct);

            var allowed = currentUser.IsSystemAdmin
                || target.Id == me                 // yourself
                || target.ManagerId == me          // your direct worker
                || myManagerId == target.Id;       // your own manager

            if (!allowed)
                throw new ForbiddenAccessException("You do not have access to view this user.");

            return target;
        }
    }
}