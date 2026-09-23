using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using TaskManager.Application.Common.Interfaces;
using TaskManager.Application.Users.Common.Interfaces;
namespace TaskManager.Application.Users.Common.Services
{
    public sealed class ManagerHierarchyService(IApplicationDbContext db) 
    : IManagerHierarchyService
    {
        public async Task<bool> WouldCreateCycleAsync(Guid workerId,Guid proposedManagerId,CancellationToken ct)
        {
            var currentId = (Guid?)proposedManagerId;
            var visited = new HashSet<Guid>();

            while (currentId is not null)
            {
                if (currentId == workerId)
                {
                    return true;
                }

                if (!visited.Add(currentId.Value))
                {
                    return true;
                }

                currentId = await db.Users
                                .Where(u=>u.Id == currentId)
                                .Select(u=>u.ManagerId)
                                .FirstOrDefaultAsync(ct);
            }

            return false;
        }
    }
}