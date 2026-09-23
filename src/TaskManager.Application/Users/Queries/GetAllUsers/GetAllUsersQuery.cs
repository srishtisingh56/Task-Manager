using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MediatR;
using TaskManager.Application.Common.Models;
using TaskManager.Application.Users.Common;

namespace TaskManager.Application.Users.Queries.GetAllUsers
{
   public sealed record GetAllUsersQuery(
    int PageNumber = 1,
    int PageSize = 20) : IRequest<PagedResult<UserDto>>;

   
}