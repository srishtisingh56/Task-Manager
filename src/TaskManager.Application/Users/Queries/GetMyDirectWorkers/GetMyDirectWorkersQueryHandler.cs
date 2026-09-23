using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MediatR;
using Microsoft.EntityFrameworkCore;
using TaskManager.Application.Common.Interfaces;
using TaskManager.Application.Users.Common;

namespace TaskManager.Application.Users.Queries.GetMyDirectWorkers
{
   public sealed class GetMyDirectWorkersQueryHandler(
    IApplicationDbContext db,
    ICurrentUserService currentUser
) : IRequestHandler<GetMyDirectWorkersQuery, List<WorkerDto>>
{
    public async Task<List<WorkerDto>> Handle(GetMyDirectWorkersQuery request, CancellationToken ct)
    {
        // No TaskId/UserId on this command — it's implicitly scoped to
        // "my" workers via ICurrentUserService, same "direct-only,
        // non-transitive" rule as everywhere else. No manual auth check
        // needed here since the filter itself enforces scope.
        return await db.Users
            .AsNoTracking()
            .Where(u => u.ManagerId == currentUser.UserId)
            .Select(u => new WorkerDto(u.Id, u.Name))
            .ToListAsync(ct);
    }
}
}