using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using StudioB2B.Domain.Entities.Orders;

namespace StudioB2B.Infrastructure.Persistence.Tenant.Configurations;

public class SyncJobScheduleConfiguration : IEntityTypeConfiguration<SyncJobSchedule>
{
    public void Configure(EntityTypeBuilder<SyncJobSchedule> builder)
    {
        builder.HasKey(x => x.Id);

        builder.Property(x => x.HangfireRecurringJobId)
            .HasMaxLength(200);

        builder.HasIndex(x => x.HangfireRecurringJobId)
            .IsUnique()
            .HasFilter("HangfireRecurringJobId IS NOT NULL");

        builder.Property(x => x.DaysOfWeek)
            .HasMaxLength(20);

        builder.Property(x => x.CronExpression)
            .HasMaxLength(200);

        builder.Property(x => x.CreatedByEmail)
            .HasMaxLength(256);
    }
}

