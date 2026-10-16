
using Microsoft.Extensions.DependencyInjection;
using Hangfire;

namespace TaskManager.Infrastructure.Jobs
{
   public static class JobScheduler
{
    public static void RegisterRecurringJobs(this IServiceProvider services)
    {
        var manager = services.GetRequiredService<IRecurringJobManager>();

        manager.AddOrUpdate<OverdueTaskScanJob>(
            "overdue-task-scan",
            job => job.RunAsync(CancellationToken.None),
            "*/5 * * * *");

        manager.AddOrUpdate<OffsetNotificationScanJob>(
            "offset-notification-scan",
            job => job.RunAsync(CancellationToken.None),
            "*/5 * * * *");
    }
}
}