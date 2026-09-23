using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MediatR;
using TaskManager.Application.Users.Common;

namespace TaskManager.Application.Users.Commands.CreateUser
{
  public sealed record CreateUserCommand(
    string Name,
    string Email,
    string PhoneNumber
    ) : IRequest<UserDto>;
}