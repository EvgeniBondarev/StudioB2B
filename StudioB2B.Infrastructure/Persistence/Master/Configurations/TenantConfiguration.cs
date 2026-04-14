using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using StudioB2B.Domain.Entities;

namespace StudioB2B.Infrastructure.Persistence.Master.Configurations;

public class TenantConfiguration : IEntityTypeConfiguration<TenantEntity>
{
    public void Configure(EntityTypeBuilder<TenantEntity> builder)
    {
        builder.ToTable("Tenants");

        builder.HasKey(t => t.Id);

        builder.Property(t => t.Name)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(t => t.Subdomain)
            .IsRequired()
            .HasMaxLength(100);

        builder.HasIndex(t => t.Subdomain)
            .IsUnique();

        builder.Property(t => t.ConnectionString)
            .IsRequired()
            .HasMaxLength(500);

        builder.Property(t => t.RequireLoginCode)
            .HasDefaultValue(true);

        builder.Property(t => t.RequireEmailActivation)
            .HasDefaultValue(true);

        builder.Property(t => t.CreatedByUserId)
            .IsRequired(false);

        builder.HasOne(t => t.CreatedBy)
            .WithMany()
            .HasForeignKey(t => t.CreatedByUserId)
            .OnDelete(DeleteBehavior.SetNull)
            .IsRequired(false);
    }
}
