using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TaskManager.Domain.Entities;

namespace TaskManager.Infrastructure.Persistence.Configurations
{
    public class NotificationRuleConfiguration : IEntityTypeConfiguration<NotificationRule>
    {
        public void Configure(EntityTypeBuilder<NotificationRule> builder)
        {
            builder.HasKey(r => r.Id);

            builder.Property(r => r.TriggerEvent)
                .IsRequired()
                .HasConversion<string>()
                .HasMaxLength(30);

            builder.Property(r => r.OffsetUnit)
                .HasConversion<string>()
                .HasMaxLength(20);

            builder.Property(r => r.RepeatMode)
                .IsRequired()
                .HasConversion<string>()
                .HasMaxLength(20);

            // A rule belongs to exactly one task; deleting the task deletes its schedules.
            builder.HasOne<TaskItem>()
                .WithMany()
                .HasForeignKey(r => r.TaskId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.HasIndex(r => r.TaskId);
        }
    }
}
