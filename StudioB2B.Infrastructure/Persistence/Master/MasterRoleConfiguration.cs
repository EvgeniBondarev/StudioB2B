using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using StudioB2B.Domain.Entities.Tenants;

namespace StudioB2B.Infrastructure.Persistence.Master;

public class MasterRoleConfiguration : IEntityTypeConfiguration<MasterRole>
{
    public void Configure(EntityTypeBuilder<MasterRole> builder)
    {
        builder.ToTable("Roles");

        builder.HasKey(r => r.Id);

        builder.Property(r => r.Name)
            .IsRequired()
            .HasMaxLength(256);

        builder.Property(r => r.NormalizedName)
            .IsRequired()
            .HasMaxLength(256);

        builder.HasIndex(r => r.NormalizedName)
            .IsUnique();

        builder.Property(r => r.ConcurrencyStamp)
            .HasMaxLength(36);

        builder.Property(r => r.Description)
            .HasMaxLength(500);

        builder.Property(r => r.CreatedAtUtc).IsRequired();
    }
}

