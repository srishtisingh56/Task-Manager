using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace TaskManager.Application.Users.Common
{
    public sealed record WorkerDto(Guid Id, string Name);
}