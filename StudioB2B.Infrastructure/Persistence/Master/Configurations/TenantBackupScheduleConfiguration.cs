using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using StudioB2B.Domain.Entities;

namespace StudioB2B.Infrastructure.Persistence.Master.Configurations;

public class TenantBackupScheduleConfiguration : IEntityTypeConfiguration<TenantBackupSchedule>
{
    public void Configure(EntityTypeBuilder<TenantBackupSchedule> builder)
    {
        builder.ToTable("TenantBackupSchedules");

        builder.HasKey(s => s.Id);

        builder.HasIndex(s => s.TenantId).IsUnique();

        builder.Property(s => s.CronExpression).IsRequired().HasMaxLength(100);

        builder.Property(s => s.HangfireJobId).HasMaxLength(200);

        builder.HasOne(s => s.Tenant)
            .WithMany()
            .HasForeignKey(s => s.TenantId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasQueryFilter(s => !s.Tenant!.IsDeleted);
    }
}

