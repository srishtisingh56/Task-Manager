using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace TaskManager.Application.Users.Common
{
   public sealed record UserDetailDto(
    Guid Id,
    string Name,
    string Email,
    string PhoneNumber,
    Guid? ManagerId,
    string? ManagerName,
    bool IsSystemAdmin,
    bool IsActive);
}