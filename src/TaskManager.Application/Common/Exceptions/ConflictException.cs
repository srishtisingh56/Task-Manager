using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace TaskManager.Application.Common.Exceptions
{
    public sealed class ConflictException(string message) : Exception(message);
}