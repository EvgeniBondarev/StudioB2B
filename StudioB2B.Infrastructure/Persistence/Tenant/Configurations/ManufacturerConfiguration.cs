using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using StudioB2B.Domain.Entities;

namespace StudioB2B.Infrastructure.Persistence.Tenant.Configurations;

public class ManufacturerConfiguration : IEntityTypeConfiguration<Manufacturer>
{
    public void Configure(EntityTypeBuilder<Manufacturer> builder)
    {
        builder.ToTable("Manufacturers");
        builder.HasKey(m => m.Id);

        builder.Property(m => m.Prefix).IsRequired().HasMaxLength(50);
        builder.Property(m => m.Name).IsRequired().HasMaxLength(300);
        builder.Property(m => m.Contact).HasMaxLength(500);
        builder.Property(m => m.Description).HasMaxLength(2000);
        builder.Property(m => m.Address).HasMaxLength(500);
        builder.Property(m => m.Website).HasMaxLength(300);
        builder.Property(m => m.ExistName).HasMaxLength(300);
        builder.Property(m => m.Domain).HasMaxLength(300);
        builder.Property(m => m.MarketPrefix).HasMaxLength(50);

        builder.HasIndex(m => m.Prefix).IsUnique();
    }
}
