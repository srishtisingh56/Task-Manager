using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using TaskManager.Domain.Enums;

namespace TaskManager.Domain.Exceptions
{
    /// <summary>
    ///Thrown when code attempts a status transition on TaskItem that isn't
    /// allowed (e.g. completing a task that's already Completed, or resuming
    /// a task that was never Halted). Keeping this check inside the entity
    /// (rather than in a handler) means the invariant holds no matter which
    /// code path tries to mutate the task — API, background job, tests, etc.
    /// </summary>
    public sealed class InvalidTaskStateTransitionException : DomainException
    {
        public InvalidTaskStateTransitionException(TaskItemStatus from,TaskItemStatus to) 
        : base($"Cannot transition state from '{from}' to '{to}'.")
        {
        }
    }
    /// <summary>
    /// Thrown when any state-changing operation (update, reassign, complete,
    /// halt, resume, delete again...) is attempted on a TaskItem that's already
    /// soft-deleted. A deleted task is meant to be inert — the only way out of
    /// this state is Restore().
    /// </summary>
    public sealed class TaskAlreadyDeletedException : DomainException
    {
        public TaskAlreadyDeletedException(Guid id) 
        : base($"Task {id} has already been deleted and cannot be modified. Restore it first if this was unintended.")
        {
        }
    }
    /// <summary>
    /// Thrown when Reassign() is called on a task that's already Completed.
    /// </summary>
    public sealed class TaskAlreadyCompletedException : DomainException
    {
        public TaskAlreadyCompletedException(Guid taskId) 
        : base($"Task {taskId} has already been completed and cannot be reassigned.")
        {
        }
    }

    public sealed class TaskNotDeletedException : DomainException
    {
        public TaskNotDeletedException(Guid taskId) 
        : base($"Task {taskId} is not deleted, so it cannot be restored.")
        {
        }
    }


}