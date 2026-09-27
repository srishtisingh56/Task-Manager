using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using TaskManager.Application.Common.Interfaces;
using TaskManager.Domain.Enums;

namespace TaskManager.Infrastructure.Jobs;

public class OverdueTaskScanJob
{
    private readonly IApplicationDbContext _db;
    private readonly IDateTime _dateTime;
    private readonly INotificationDispatcher _dispatcher;
    private readonly ILogger<OverdueTaskScanJob> _logger;

    public OverdueTaskScanJob(
        IApplicationDbContext db,
        IDateTime dateTime,
        INotificationDispatcher dispatcher,
        ILogger<OverdueTaskScanJob> logger)
    {
        _db = db;
        _dateTime = dateTime;
        _dispatcher = dispatcher;
        _logger = logger;
    }

    public async Task RunAsync(CancellationToken ct)
    {
        var now = _dateTime.UtcNow;

        var overdueTasks = await _db.TaskItems
            .Where(t => (t.Status == TaskItemStatus.Pending || t.Status == TaskItemStatus.Halted)
                        && t.StrictDeadline < now)
            .ToListAsync(ct);

        if (overdueTasks.Count == 0)
            return;

        foreach (var task in overdueTasks)
            task.MarkOverdue(); // idempotent, defensive no-op on already-terminal/deleted per domain_layer.md

        await _db.SaveChangesAsync(ct);

        // Dispatch AFTER save succeeds, per the dispatch contract — a failed send must never
        // roll back the already-committed status transition.
        foreach (var task in overdueTasks)
        {
            try
            {
                await _dispatcher.DispatchAsync(task.Id, task.CreatedByUserId,
                    NotificationTriggerEvent.StrictDeadlinePassed, ct);
            }
            catch (Exception ex)
            {
                // Dispatcher already swallows its own internal failures; this catch is a last-resort
                // net in case something upstream of the log write throws unexpectedly.
                _logger.LogError(ex, "Dispatch failed for overdue task {TaskId}", task.Id);
            }
        }

        _logger.LogInformation("Overdue scan processed {Count} tasks", overdueTasks.Count);
    }
}