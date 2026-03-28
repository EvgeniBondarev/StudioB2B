using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using StudioB2B.Domain.Entities;

namespace StudioB2B.Infrastructure.Persistence.Tenant.Configurations;

public class CommunicationTaskConfiguration : IEntityTypeConfiguration<CommunicationTask>
{
    public void Configure(EntityTypeBuilder<CommunicationTask> builder)
    {
        builder.ToTable("CommunicationTasks");
        builder.HasKey(x => x.Id);

        builder.Property(x => x.TaskType).IsRequired();
        builder.Property(x => x.ExternalId).IsRequired().HasMaxLength(256);
        builder.Property(x => x.Status).IsRequired();

        builder.Property(x => x.Title).IsRequired().HasMaxLength(500);
        builder.Property(x => x.PreviewText).HasMaxLength(2000);
        builder.Property(x => x.ExternalStatus).HasMaxLength(100);
        builder.Property(x => x.ChatType).HasMaxLength(50);
        builder.Property(x => x.ExternalUrl).HasMaxLength(1000);

        builder.Property(x => x.PaymentAmount).HasColumnType("decimal(18,2)");

        builder.HasOne(x => x.MarketplaceClient)
            .WithMany()
            .HasForeignKey(x => x.MarketplaceClientId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(x => x.AssignedToUser)
            .WithMany()
            .HasForeignKey(x => x.AssignedToUserId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasIndex(x => new { x.TaskType, x.ExternalId, x.MarketplaceClientId })
            .IsUnique();

        builder.HasIndex(x => x.Status);
        builder.HasIndex(x => x.AssignedToUserId);
        builder.HasIndex(x => x.CreatedAt);
    }
}
