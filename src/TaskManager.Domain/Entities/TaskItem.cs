using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using TaskManager.Domain.Common;
using TaskManager.Domain.Enums;
using TaskManager.Domain.Exceptions;

namespace TaskManager.Domain.Entities
{
    /// <summary>
    /// Represents a task item within the task management system.
    /// This class has properties for Title, Description, Priority, LenientDeadline, StrictDeadline, IsRepetitive, CreatedByUserId, and AssignedToUserId.
    /// </summary>
    public sealed class TaskItem : Entity
    {
        //use null! -> suppress compiler's null warning -> tells right now its null, but later would be initialized
        public string Title { get; private set; } = null!;
        public string? Description { get; private set;} 

        public TaskItemStatus Status { get; private set; }
        public TaskPriority? Priority { get; private set; }
        public DateTime LenientDeadline { get; private set; }
        public DateTime StrictDeadline { get; private set; }
        public bool IsRepetitive {get; private set; }

        public Guid CreatedByUserId { get; private set; }
        public Guid AssignedToUserId { get; private set; }
        public bool IsDeleted { get; private set; }
        public DateTime? DeletedAt { get; private set; }

        private TaskItem() { }

        private TaskItem(
            Guid id,
            string title,
            string? description,
            TaskPriority? priority,
            DateTime lenientDeadline,
            DateTime strictDeadline,
            bool isRepetitive,
            Guid createdByUserId,
            Guid assignedToUserId)
            : base(id)  
        {
            Title = title;
            Description = description;
            Priority = priority;
            LenientDeadline = lenientDeadline;
            StrictDeadline = strictDeadline;
            IsRepetitive = isRepetitive;
            CreatedByUserId = createdByUserId;
            AssignedToUserId = assignedToUserId;
            Status = TaskItemStatus.Pending;
        }

        //left:check that lenient and strict deadline should not be empty 
        /// <summary>
        /// Creates a new task item with the specified details.
        /// </summary>
        public static TaskItem Create(
            string title,
            string? description,
            DateTime lenientDeadline,
            DateTime strictDeadline,
            bool isRepetitive,
            Guid createdByUserId,
            Guid assignedToUserId,
            TaskPriority? priority = null)
        {
            title = title?.Trim() ?? string.Empty;

            if(string.IsNullOrWhiteSpace(title))
            {
                throw new ArgumentException("Title cannot be empty.", nameof(title));
            }

            if(createdByUserId == Guid.Empty)
            {
                throw new ArgumentException("CreatedByUserId cannot be empty.", nameof(createdByUserId));
            }

            if(assignedToUserId == Guid.Empty)
            {
                throw new ArgumentException("AssignedToUserId cannot be empty.", nameof(assignedToUserId));
            }

            ValidateDeadlines(lenientDeadline, strictDeadline);

            return new TaskItem(
                Guid.NewGuid(),
                title,
                string.IsNullOrWhiteSpace(description) ? null : description,
                priority,
                lenientDeadline,
                strictDeadline,
                isRepetitive,
                createdByUserId,
                assignedToUserId);
        }
        /// <summary>
        /// Validates that the lenient deadline is earlier than the strict deadline.
        /// </summary>
        private static void ValidateDeadlines(DateTime lenientDeadline, DateTime strictDeadline)
        {
            if(lenientDeadline >= strictDeadline)
            {
                throw new InvalidTaskDeadlineException(lenientDeadline, strictDeadline);
            }
        }
        /// <summary>
        /// Ensures that the task has not been deleted.
        /// </summary>
        private void EnsureNotDeleted()
        {
            if(IsDeleted)
            {
                throw new TaskAlreadyDeletedException(Id);
            }
        }
        
        //Kept as is -> not changed 
        /// <summary>
        /// Updates the title, description, and priority of the task.
        /// </summary>
        public void UpdateDetails(string title, string? description, TaskPriority? priority)
        {
            EnsureNotDeleted();

            title = title?.Trim() ?? string.Empty;
            if (string.IsNullOrWhiteSpace(title))
            {
                throw new ArgumentException("Title is required.", nameof(title));
            }

            Title = title;
            Description = string.IsNullOrWhiteSpace(description) ? null : description.Trim();
            Priority = priority;
        }
        /// <summary>
        /// Updates the lenient and strict deadlines for the task.
        /// </summary>
        public void UpdateDeadlines(DateTime lenientDeadline, DateTime strictDeadline)
        {
            EnsureNotDeleted();
            ValidateDeadlines(lenientDeadline, strictDeadline);
            LenientDeadline = lenientDeadline;
            StrictDeadline = strictDeadline;
        }
        /// <summary>
        /// Sets whether the task is repetitive.
        /// </summary>
        public void SetRepetitive(bool isRepetitive)
        {
            EnsureNotDeleted();
            IsRepetitive = isRepetitive;
        }
        /// <summary>
        /// Reassigns the task to a new user.
        /// </summary>
        public void Reassign(Guid newAssignedToUserId)
        {
            EnsureNotDeleted();
            if(newAssignedToUserId == Guid.Empty)
            {
                throw new ArgumentException("New assigned user ID cannot be empty.", nameof(newAssignedToUserId));
            }
            if(Status == TaskItemStatus.Completed)
            {
                throw new TaskAlreadyCompletedException(Id);
            }
            AssignedToUserId = newAssignedToUserId;
        }
        /// <summary>
        /// Marks the task as completed.
        /// </summary>
        public void MarkCompleted()
        {
            EnsureNotDeleted();
            if(Status == TaskItemStatus.Completed)
            {
                throw new InvalidTaskStateTransitionException(Status, TaskItemStatus.Completed);
            }
            // Pending, Halted, and Overdue can all transition to Completed —
            // "late but done" is still a real, valid outcome 
            Status = TaskItemStatus.Completed;
        }
        /// <summary>
        /// Halts a task that is currently pending.
        /// </summary>
        public void Halt()
        {
            EnsureNotDeleted();
            if(Status != TaskItemStatus.Pending)
            {
                throw new InvalidTaskStateTransitionException(Status, TaskItemStatus.Halted);
            }
            Status = TaskItemStatus.Halted;
        }

        /// <summary>
        /// Resumes a task that is currently halted.
        /// </summary>
        public void Resume()
        {
            EnsureNotDeleted();
            if(Status != TaskItemStatus.Halted)
            {
                throw new InvalidTaskStateTransitionException(Status, TaskItemStatus.Pending);
            }
            Status = TaskItemStatus.Pending;
        }
        /// <summary>
        /// Soft Deletes the task.
        /// </summary>
        public void Delete()
        {
            EnsureNotDeleted();
            IsDeleted = true;
            DeletedAt = DateTime.UtcNow;
        }
        /// <summary>
        /// Restores a previously soft-deleted task.
        /// </summary>
        public void Restore()
        {
            if(!IsDeleted)
            {
                throw new TaskNotDeletedException(Id);
            }
            IsDeleted = false;
            DeletedAt = null;
        }
         /// <summary>
        /// System-driven transition: called by the Hangfire recurring job that
        /// scans for tasks past StrictDeadline, never by a user directly. Kept
        /// idempotent (safe to call repeatedly) since a background scan running
        /// more than once shouldn't throw -
        /// In practice the query feeding this job already filters
        /// IsDeleted == false, so this is a defensive second line, not the
        /// primary guard.
        /// </summary>
        public void MarkOverdue()
        {
            if(IsDeleted || Status is TaskItemStatus.Completed or TaskItemStatus.Overdue)
            {
                throw new InvalidTaskStateTransitionException(Status, TaskItemStatus.Overdue);
            }
            Status = TaskItemStatus.Overdue;
        }

        /// <summary>
        /// Checks if the task is past its strict deadline as of the given UTC time.
        /// </summary>
        public bool IsPastStrictDeadline(DateTime asOfUtc) =>
            Status is TaskItemStatus.Pending && StrictDeadline < asOfUtc;

        /// <summary>
        /// Checks if the task is past its lenient deadline as of the given UTC time.
        /// </summary>
        public bool IsPastLenientDeadline(DateTime asOfUtc) =>
            Status is TaskItemStatus.Pending && LenientDeadline < asOfUtc;


    }
}
