using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using StudioB2B.Domain.Entities.Orders;

namespace StudioB2B.Infrastructure.Persistence.Tenant.Configurations;

public class SyncJobHistoryConfiguration : IEntityTypeConfiguration<SyncJobHistory>
{
    public void Configure(EntityTypeBuilder<SyncJobHistory> builder)
    {
        builder.ToTable("SyncJobHistories");
        builder.HasKey(x => x.Id);

        builder.Property(x => x.HangfireJobId)
            .IsRequired()
            .HasMaxLength(100);

        builder.HasIndex(x => x.HangfireJobId);

        builder.Property(x => x.JobType)
            .IsRequired();

        builder.Property(x => x.Status)
            .IsRequired();

        builder.Property(x => x.StartedAtUtc)
            .IsRequired();

        builder.Property(x => x.ResultJson)
            .HasColumnType("longtext");

        builder.Property(x => x.ErrorMessage)
            .HasMaxLength(2000);
    }
}

