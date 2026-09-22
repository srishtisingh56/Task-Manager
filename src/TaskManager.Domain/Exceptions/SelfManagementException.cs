using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using TaskManager.Domain.Common;

namespace TaskManager.Domain.Exceptions
{
    public sealed class SelfManagementException : DomainException
    {
        public SelfManagementException(Guid userId)
            : base($"User '{userId}' cannot be assigned as their own manager.")
        {
        }
    }
}