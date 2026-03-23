using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using StudioB2B.Domain.Entities;

namespace StudioB2B.Infrastructure.Persistence.Tenant.Configurations;

public class AppFunctionConfiguration : IEntityTypeConfiguration<AppFunction>
{
    public void Configure(EntityTypeBuilder<AppFunction> builder)
    {
        builder.ToTable("Functions");
        builder.HasKey(f => f.Id);
        builder.Property(f => f.Id).ValueGeneratedNever();
        builder.Property(f => f.Name).IsRequired().HasMaxLength(100);
        builder.Property(f => f.DisplayName).IsRequired().HasMaxLength(200).HasDefaultValue("");
        builder.HasIndex(f => f.Name).IsUnique();

        builder.HasOne(f => f.Page)
            .WithMany(p => p.Functions)
            .HasForeignKey(f => f.PageId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
