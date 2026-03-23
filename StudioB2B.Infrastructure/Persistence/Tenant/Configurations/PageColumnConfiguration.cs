using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using StudioB2B.Domain.Entities;

namespace StudioB2B.Infrastructure.Persistence.Tenant.Configurations;

public class PageColumnConfiguration : IEntityTypeConfiguration<PageColumn>
{
    public void Configure(EntityTypeBuilder<PageColumn> builder)
    {
        builder.ToTable("PageColumns");
        builder.HasKey(c => c.Id);
        builder.Property(c => c.Id).ValueGeneratedNever();
        builder.Property(c => c.Name).IsRequired().HasMaxLength(100);
        builder.Property(c => c.DisplayName).IsRequired().HasMaxLength(200).HasDefaultValue("");
        builder.HasIndex(c => c.Name).IsUnique();

        builder.HasOne(c => c.Page)
            .WithMany(p => p.Columns)
            .HasForeignKey(c => c.PageId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
