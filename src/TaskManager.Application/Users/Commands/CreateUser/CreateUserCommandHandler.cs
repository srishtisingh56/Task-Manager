using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using TaskManager.Application.Common.Interfaces;
using MediatR;
using TaskManager.Application.Users.Common;
using TaskManager.Domain.Entities;
using TaskManager.Application.Common.Exceptions;

namespace TaskManager.Application.Users.Commands.CreateUser
{
    public class CreateUserCommandHandler(
        IApplicationDbContext db,
        ICurrentUserService currentUser
    ): IRequestHandler<CreateUserCommand, UserDto>
    {
        public async Task<UserDto> Handle(CreateUserCommand request, CancellationToken ct)
        {
            if(!currentUser.IsSystemAdmin)
            {
                throw new ForbiddenAccessException("Only a system admin can create users");
            }
            var user = User.Create(request.Name, request.Email, request.PhoneNumber);
            db.Users.Add(user);
            await db.SaveChangesAsync(ct);

            return UserDto.FromEntity(user);
        }
    }
}