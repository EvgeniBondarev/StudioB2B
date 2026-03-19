using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using StudioB2B.Domain.Entities;

namespace StudioB2B.Infrastructure.Persistence.Tenant.Configurations;

public class TenantModuleConfiguration : IEntityTypeConfiguration<TenantModule>
{
    public void Configure(EntityTypeBuilder<TenantModule> builder)
    {
        builder.ToTable("TenantModules");
        builder.HasKey(m => m.Id);

        builder.Property(m => m.Code).IsRequired().HasMaxLength(100);
        builder.Property(m => m.Name).IsRequired().HasMaxLength(200);
        builder.Property(m => m.Description).HasMaxLength(1000);

        builder.HasIndex(m => m.Code).IsUnique();
    }
}
