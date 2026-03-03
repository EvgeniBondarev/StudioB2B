using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TenantEntity = StudioB2B.Domain.Entities.Tenants.Tenant;

namespace StudioB2B.Infrastructure.Persistence.Master;

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
    }
}
