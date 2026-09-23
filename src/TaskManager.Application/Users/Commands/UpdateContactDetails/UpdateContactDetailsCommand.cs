using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MediatR;
using TaskManager.Application.Common.Models;
using TaskManager.Application.Users.Common;

namespace TaskManager.Application.Users.Commands.UpdateContactDetails
{
    public sealed record UpdateContactDetailsCommand
    (
        Guid UserId,
        Optional<string> Name,
        Optional<string> Email,
        Optional<string> PhoneNumber 
    ) : IRequest<UserDto>;
}