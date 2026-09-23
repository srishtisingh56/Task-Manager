using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MediatR;
using TaskManager.Application.Common.Exceptions;
using TaskManager.Application.Common.Interfaces;
using TaskManager.Application.Users.Common;
using TaskManager.Application.Users.Common.Interfaces;
using TaskManager.Domain.Entities;

namespace TaskManager.Application.Users.Commands.AssignManager
{
    public sealed class AssignManagerCommandHandler
    (
        IApplicationDbContext db,
        ICurrentUserService currentUser,
        IManagerHierarchyService managerHierarchyService
    ) : IRequestHandler<AssignManagerCommand, UserDto>
    {
        public async Task<UserDto> Handle(AssignManagerCommand request, CancellationToken ct)
        {
            var worker = await db.Users.FindAsync([request.WorkerId], ct)
            ?? throw new NotFoundException(nameof(User),request.WorkerId);

            var manager = await db.Users.FindAsync([request.ManagerId], ct)
            ?? throw new NotFoundException(nameof(User),request.ManagerId);

            if(!currentUser.IsSystemAdmin)
            {
                throw new ForbiddenAccessException("Only a system admin can assign managers");
            }

            if(await managerHierarchyService.WouldCreateCycleAsync(worker.Id, manager.Id, ct))
            {
                throw new ForbiddenAccessException("Assigning this manager would create a cycle in the manager hierarchy");
            }

            worker.AssignManager(manager);

            await db.SaveChangesAsync(ct);

            return UserDto.FromEntity(worker);
        }
    }
}