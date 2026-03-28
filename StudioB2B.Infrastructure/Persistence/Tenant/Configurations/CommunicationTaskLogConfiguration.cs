using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using StudioB2B.Domain.Entities;

namespace StudioB2B.Infrastructure.Persistence.Tenant.Configurations;

public class CommunicationTaskLogConfiguration : IEntityTypeConfiguration<CommunicationTaskLog>
{
    public void Configure(EntityTypeBuilder<CommunicationTaskLog> builder)
    {
        builder.ToTable("CommunicationTaskLogs");
        builder.HasKey(x => x.Id);

        builder.Property(x => x.Action).IsRequired().HasMaxLength(100);
        builder.Property(x => x.Details).HasColumnType("longtext");

        builder.HasOne(x => x.Task)
            .WithMany(t => t.Logs)
            .HasForeignKey(x => x.TaskId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(x => x.User)
            .WithMany()
            .HasForeignKey(x => x.UserId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasIndex(x => x.TaskId);
        builder.HasIndex(x => x.CreatedAt);
    }
}
