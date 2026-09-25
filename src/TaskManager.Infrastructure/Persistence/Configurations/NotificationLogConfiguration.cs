using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TaskManager.Domain.Entities;

namespace TaskManager.Infrastructure.Persistence.Configurations
{
    public class NotificationLogConfiguration : IEntityTypeConfiguration<NotificationLog>
    {
        public void Configure(EntityTypeBuilder<NotificationLog> builder)
        {
            builder.HasKey(l => l.Id);

            builder.Property(l => l.Channel)
                .IsRequired()
                .HasConversion<string>()
                .HasMaxLength(20);

            builder.Property(l => l.DeliveryStatus)
                .IsRequired()
                .HasConversion<string>()
                .HasMaxLength(20);

            builder.Property(l => l.SentAt)
                .IsRequired();

            // Audit trail belongs to the task; deleting the task removes its log history too.
            builder.HasOne<TaskItem>()
                .WithMany()
                .HasForeignKey(l => l.TaskId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.HasOne<User>()
                .WithMany()
                .HasForeignKey(l => l.RecipientUserId)
                .OnDelete(DeleteBehavior.Restrict);

            // Nullable: rule can be deleted (or the log is from an immediate/system event) without losing the log.
            builder.HasOne<NotificationRule>()
                .WithMany()
                .HasForeignKey(l => l.NotificationRuleId)
                .OnDelete(DeleteBehavior.SetNull);

            builder.HasIndex(l => l.TaskId);
            builder.HasIndex(l => l.RecipientUserId);
        }
    }
}
