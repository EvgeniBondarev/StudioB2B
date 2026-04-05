using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using StudioB2B.Domain.Entities;
namespace StudioB2B.Infrastructure.Persistence.Master.Configurations;
public class TenantRestoreHistoryConfiguration : IEntityTypeConfiguration<TenantRestoreHistory>
{
    public void Configure(EntityTypeBuilder<TenantRestoreHistory> builder)
    {
        builder.ToTable("TenantRestoreHistories");
        builder.HasKey(h => h.Id);
        builder.HasIndex(h => h.TenantId);
        builder.HasIndex(h => h.StartedAtUtc);
        builder.Property(h => h.SourceObjectKey).IsRequired().HasMaxLength(500);
        builder.Property(h => h.SourceType).IsRequired().HasMaxLength(50);
        builder.Property(h => h.Status).IsRequired().HasMaxLength(50);
        builder.Property(h => h.ErrorMessage).HasMaxLength(2000);
        builder.HasOne<TenantEntity>()
            .WithMany()
            .HasForeignKey(h => h.TenantId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
