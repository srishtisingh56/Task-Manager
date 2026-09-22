using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace TaskManager.Application.Common.Interfaces
{
    public interface ICurrentUserService
    {
        Guid UserId { get; }
        bool IsSystemAdmin { get; }
    }
}