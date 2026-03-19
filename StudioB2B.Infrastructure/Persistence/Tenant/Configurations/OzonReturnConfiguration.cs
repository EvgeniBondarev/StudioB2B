using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using StudioB2B.Domain.Entities.Orders;

namespace StudioB2B.Infrastructure.Persistence.Tenant.Configurations;

public class OzonReturnConfiguration : IEntityTypeConfiguration<OrderReturn>
{
    public void Configure(EntityTypeBuilder<OrderReturn> builder)
    {
        builder.ToTable("OrderReturns");
        builder.HasKey(r => r.Id);

        builder.HasIndex(r => r.OzonReturnId).IsUnique();
        builder.HasIndex(r => r.OzonOrderId);
        builder.HasIndex(r => r.PostingNumber);
        builder.HasIndex(r => r.VisualStatusSysName);
        builder.HasIndex(r => r.OrderId);
        builder.HasIndex(r => r.ShipmentId);

        builder.Property(r => r.ReturnReasonName).HasMaxLength(500);
        builder.Property(r => r.Type).HasMaxLength(50);
        builder.Property(r => r.Schema).HasMaxLength(10);
        builder.Property(r => r.OrderNumber).HasMaxLength(200);
        builder.Property(r => r.PostingNumber).HasMaxLength(200);
        builder.Property(r => r.OfferId).HasMaxLength(200);
        builder.Property(r => r.ProductName).HasMaxLength(500);
        builder.Property(r => r.ProductPriceCurrencyCode).HasMaxLength(10);
        builder.Property(r => r.StorageCurrencyCode).HasMaxLength(10);
        builder.Property(r => r.LogisticBarcode).HasMaxLength(100);
        builder.Property(r => r.VisualStatusDisplayName).HasMaxLength(200);
        builder.Property(r => r.VisualStatusSysName).HasMaxLength(100);
        builder.Property(r => r.CompensationStatusDisplayName).HasMaxLength(200);
        builder.Property(r => r.PlaceName).HasMaxLength(500);
        builder.Property(r => r.PlaceAddress).HasMaxLength(1000);

        builder.Property(r => r.ProductPrice).HasPrecision(18, 4);
        builder.Property(r => r.ProductPriceWithoutCommission).HasPrecision(18, 4);
        builder.Property(r => r.CommissionPercent).HasPrecision(10, 4);
        builder.Property(r => r.Commission).HasPrecision(18, 4);
        builder.Property(r => r.StorageSum).HasPrecision(18, 4);
        builder.Property(r => r.UtilizationSum).HasPrecision(18, 4);

        builder.HasOne(r => r.Order)
            .WithMany(o => o.Returns)
            .HasForeignKey(r => r.OrderId)
            .OnDelete(DeleteBehavior.SetNull);
    }
}
