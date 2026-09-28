using Hangfire;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using TaskManager.Application.Common.Interfaces;
using TaskManager.Domain.Entities;
using TaskManager.Domain.Enums;

namespace TaskManager.Infrastructure.Jobs;

public class OffsetNotificationScanJob
{
    private readonly IApplicationDbContext _db;
    private readonly IDateTime _dateTime;
    private readonly INotificationDispatcher _dispatcher;
    private readonly ILogger<OffsetNotificationScanJob> _logger;

    public OffsetNotificationScanJob(
        IApplicationDbContext db,
        IDateTime dateTime,
        INotificationDispatcher dispatcher,
        ILogger<OffsetNotificationScanJob> logger)
    {
        _db = db;
        _dateTime = dateTime;
        _dispatcher = dispatcher;
        _logger = logger;
    }

   [DisableConcurrentExecution(timeoutInSeconds: 300)]
   public async Task RunAsync(CancellationToken ct)
{
    var now = _dateTime.UtcNow;

    var rules = await _db.NotificationRules.ToListAsync(ct);
    if (rules.Count == 0)
        return;

    var taskIds = rules.Select(r => r.TaskId).Distinct().ToList();
    var tasks = await _db.TaskItems
        .Where(t => !t.IsDeleted  && taskIds.Contains(t.Id) && t.Status != TaskItemStatus.Completed)
        .ToDictionaryAsync(t => t.Id, ct);

    foreach (var rule in rules)
    {
        if (!tasks.TryGetValue(rule.TaskId, out var task))
            continue; // task completed, soft-deleted (filtered out), or gone

        var referencePoint = rule.TriggerEvent switch
        {
            NotificationTriggerEvent.AfterCreationOffset => task.CreatedAt,
            NotificationTriggerEvent.BeforeLenientDeadline => task.LenientDeadline,
            NotificationTriggerEvent.BeforeStrictDeadline => task.StrictDeadline,
            _ => throw new InvalidOperationException(
                $"Unexpected trigger event {rule.TriggerEvent} on NotificationRule {rule.Id}")
        };

        if (rule.RepeatMode == NotificationRepeatMode.Once)
        {
            var alreadySent = await _db.NotificationLogs
                .AnyAsync(l => l.NotificationRuleId == rule.Id, ct);
            if (alreadySent)
                continue;

            var fireTime = rule.CalculateFireTime(referencePoint);
            if (fireTime <= now)
                await FireAsync(rule, task, ct);
        }
        else
        {
            var lastLog = await _db.NotificationLogs
                .Where(l => l.NotificationRuleId == rule.Id)
                .OrderByDescending(l => l.SentAt)
                .FirstOrDefaultAsync(ct);

            var lastPoint = lastLog?.SentAt ?? referencePoint;
            var nextFireTime = rule.CalculateFireTime(lastPoint);

            if (nextFireTime <= now)
                await FireAsync(rule, task, ct);
        }
    }
}

    private async Task FireAsync(NotificationRule rule, TaskItem task, CancellationToken ct)
    {
        try
        {
            await _dispatcher.DispatchAsync(task.Id, task.AssignedToUserId, rule.TriggerEvent, ct);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Dispatch failed for NotificationRule {RuleId}", rule.Id);
        }
    }
}