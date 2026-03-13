using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using StudioB2B.Domain.Entities;

namespace StudioB2B.Infrastructure.Persistence.Master.Configurations;

public class MasterRoleConfiguration : IEntityTypeConfiguration<MasterRole>
{
    public void Configure(EntityTypeBuilder<MasterRole> builder)
    {
        builder.ToTable("Roles");

        builder.HasKey(r => r.Id);

        builder.Property(r => r.Name)
            .IsRequired()
            .HasMaxLength(256);

        builder.HasIndex(r => r.Name)
            .IsUnique();
    }
}
