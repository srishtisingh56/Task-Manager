using Microsoft.EntityFrameworkCore;
using TaskManager.Domain.Entities;

namespace TaskManager.Application.Common.Interfaces
{
    public interface IApplicationDbContext
    {
        DbSet<User> Users { get; }
        DbSet<TaskItem> TaskItems { get; }
        DbSet<NotificationLog> NotificationLogs { get; }
        DbSet<NotificationRule> NotificationRules { get; }
        Task<int> SaveChangesAsync(CancellationToken cancellationToken);
    }
}