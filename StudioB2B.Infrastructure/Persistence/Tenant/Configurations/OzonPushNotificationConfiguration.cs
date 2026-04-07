using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using StudioB2B.Domain.Entities;

namespace StudioB2B.Infrastructure.Persistence.Tenant.Configurations;

public class OzonPushNotificationConfiguration : IEntityTypeConfiguration<OzonPushNotification>
{
    public void Configure(EntityTypeBuilder<OzonPushNotification> builder)
    {
        builder.ToTable("OzonPushNotifications");
        builder.HasKey(x => x.Id);

        builder.Property(x => x.MessageType).IsRequired().HasMaxLength(100);
        builder.Property(x => x.RawPayload).IsRequired().HasColumnType("longtext");
        builder.Property(x => x.PostingNumber).HasMaxLength(256);
        builder.Property(x => x.ReceivedAtUtc).IsRequired();

        builder.HasOne(x => x.MarketplaceClient)
            .WithMany()
            .HasForeignKey(x => x.MarketplaceClientId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasIndex(x => x.MessageType);
        builder.HasIndex(x => x.ReceivedAtUtc);
        builder.HasIndex(x => x.SellerId);
        builder.HasIndex(x => x.MarketplaceClientId);
    }
}

