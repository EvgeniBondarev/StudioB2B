using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using StudioB2B.Domain.Entities;

namespace StudioB2B.Infrastructure.Persistence.Tenant.Configurations;

public class BlockedEntityConfiguration : IEntityTypeConfiguration<BlockedEntity>
{
    public void Configure(EntityTypeBuilder<BlockedEntity> builder)
    {
        builder.ToTable("BlockedEntities");
        builder.HasKey(b => b.Id);

        builder.Property(b => b.EntityType).IsRequired();
        builder.Property(b => b.EntityId).IsRequired();

        builder.HasOne(b => b.Permission)
            .WithMany(p => p.BlockedEntities)
            .HasForeignKey(b => b.PermissionId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(b => new { b.PermissionId, b.EntityType, b.EntityId }).IsUnique();
    }
}

