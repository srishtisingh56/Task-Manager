using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace TaskManager.Application.Common.Interfaces
{
// Wraps the system clock so handlers/future Hangfire jobs never call
// DateTime.UtcNow directly — makes anything using "now" (overdue checks,
// notification fire-time comparisons) deterministically unit-testable
// by injecting a fake clock in tests.
    public interface IDateTime
    {
        DateTime UtcNow { get; }
    }
}