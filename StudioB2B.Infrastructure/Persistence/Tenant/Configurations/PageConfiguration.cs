using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using StudioB2B.Domain.Entities;

namespace StudioB2B.Infrastructure.Persistence.Tenant.Configurations;

public class PageConfiguration : IEntityTypeConfiguration<Page>
{
    public void Configure(EntityTypeBuilder<Page> builder)
    {
        builder.ToTable("Pages");
        builder.HasKey(p => p.Id);
        builder.Property(p => p.Id).ValueGeneratedNever(); // seeded from enum int values
        builder.Property(p => p.Name).IsRequired().HasMaxLength(100);
        builder.Property(p => p.DisplayName).IsRequired().HasMaxLength(200).HasDefaultValue("");
        builder.HasIndex(p => p.Name).IsUnique();
    }
}
