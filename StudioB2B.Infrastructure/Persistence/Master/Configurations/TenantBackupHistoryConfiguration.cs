using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using StudioB2B.Domain.Entities;

namespace StudioB2B.Infrastructure.Persistence.Master.Configurations;

public class TenantBackupHistoryConfiguration : IEntityTypeConfiguration<TenantBackupHistory>
{
    public void Configure(EntityTypeBuilder<TenantBackupHistory> builder)
    {
        builder.ToTable("TenantBackupHistories");

        builder.HasKey(h => h.Id);

        builder.HasIndex(h => h.TenantId);

        builder.HasIndex(h => h.StartedAtUtc);

        builder.Property(h => h.MinioObjectKey).HasMaxLength(500);

        builder.Property(h => h.Status).IsRequired().HasMaxLength(50);

        builder.Property(h => h.ErrorMessage).HasMaxLength(2000);

        builder.HasOne(h => h.Tenant)
            .WithMany()
            .HasForeignKey(h => h.TenantId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}

