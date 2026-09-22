using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace TaskManager.Domain.Exceptions
{
    public sealed class InvalidTaskDeadlineException : DomainException
    {
        public InvalidTaskDeadlineException(DateTime lenientDeadline, DateTime strictDeadline) 
        : base($"LenientDeadline ({lenientDeadline:0}) must be earlier than StrictDeadline ({strictDeadline:O}).")
        {
        }
    }
}