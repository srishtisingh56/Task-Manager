using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using TaskManager.Domain.Entities;

namespace TaskManager.Application.Users.Common
{
    public sealed record UserDto(
        Guid Id,
        string Name,
        string Email,
        string PhoneNumber,
        Guid? ManagerId,
        bool IsSystemAdmin
    )
    {
        public static UserDto FromEntity(User user)
        {
            return new UserDto(
                user.Id,
                user.Name,
                user.Email,
                user.PhoneNumber,
                user.ManagerId,
                user.IsSystemAdmin
            );
        }
    }
}