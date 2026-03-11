using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using StudioB2B.Domain.Entities;

namespace StudioB2B.Infrastructure.Persistence.Tenant.Configurations;

public class RoleConfiguration : IEntityTypeConfiguration<TenantRole>
{
    public void Configure(EntityTypeBuilder<TenantRole> builder)
    {
        builder.ToTable("Roles");

        builder.HasKey(r => r.Id);

        builder.Property(r => r.Name).IsRequired().HasMaxLength(256);
        builder.HasIndex(r => r.Name).IsUnique();
    }
}

