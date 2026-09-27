using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MediatR;
using TaskManager.Application.Users.Common;

namespace TaskManager.Application.Users.Commands
{
   
    public sealed record DeactivateUserCommand(Guid UserId) : IRequest<UserDto>;
    
}