using TaskManager.Application.Common.Exceptions;
using TaskManager.Application.Users.Common;
using MediatR;
using Microsoft.EntityFrameworkCore;
using TaskManager.Application.Common.Interfaces;
using TaskManager.Application.Common.Models;

namespace TaskManager.Application.Users.Queries.GetAllUsers
{
   public sealed class GetAllUsersQueryHandler(
    IApplicationDbContext db,
    ICurrentUserService currentUser
) : IRequestHandler<GetAllUsersQuery, PagedResult<UserDto>>
{
    public async Task<PagedResult<UserDto>> Handle(GetAllUsersQuery request, CancellationToken ct)
    {
        if (!currentUser.IsSystemAdmin)
        {
            throw new ForbiddenAccessException("Only a system admin can view all users.");
        }

        var query = db.Users.AsNoTracking();

        var totalCount = await query.CountAsync(ct);

        var items = await query
            .OrderBy(u => u.Name)
            .Skip((request.PageNumber - 1) * request.PageSize)
            .Take(request.PageSize)
            .Select(u => new UserDto(u.Id, u.Name, u.Email, u.PhoneNumber, u.ManagerId, u.IsSystemAdmin))
            .ToListAsync(ct);

        return new PagedResult<UserDto>(items, totalCount, request.PageNumber, request.PageSize);
    }
}
}