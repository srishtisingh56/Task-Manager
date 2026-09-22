using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace TaskManager.Domain.Exceptions
{
    public sealed class InvalidNotificationRuleException : DomainException
    {
        public InvalidNotificationRuleException(string message) : base(message)
        {
        }
    }
}