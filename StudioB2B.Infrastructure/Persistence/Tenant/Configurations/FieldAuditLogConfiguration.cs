using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using StudioB2B.Domain.Entities.Common;

namespace StudioB2B.Infrastructure.Persistence.Tenant.Configurations;

public class FieldAuditLogConfiguration : IEntityTypeConfiguration<FieldAuditLog>
{
    public void Configure(EntityTypeBuilder<FieldAuditLog> builder)
    {
        builder.ToTable("FieldAuditLogs");
        builder.HasKey(x => x.Id);

        builder.Property(x => x.EntityName)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(x => x.EntityId)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(x => x.FieldName)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(x => x.ChangeType)
            .IsRequired()
            .HasMaxLength(50);

        builder.Property(x => x.OldValue)
            .HasColumnType("longtext");

        builder.Property(x => x.NewValue)
            .HasColumnType("longtext");

        builder.Property(x => x.ChangedByUserName)
            .HasMaxLength(256);

        builder.HasIndex(x => new { x.EntityName, x.EntityId });
        builder.HasIndex(x => x.ChangedAtUtc);
    }
}

