using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using StudioB2B.Domain.Entities;
using StudioB2B.Domain.Entities.Orders;

namespace StudioB2B.Infrastructure.Persistence.Tenant.Configurations;

public class ShipmentConfiguration : IEntityTypeConfiguration<Shipment>
{
    public void Configure(EntityTypeBuilder<Shipment> builder)
    {
        builder.ToTable("Shipments");
        builder.HasKey(s => s.Id);

        builder.Property(s => s.PostingNumber)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(s => s.OrderNumber)
            .HasMaxLength(100);

        builder.Property(s => s.TrackingNumber)
            .HasMaxLength(200);

        builder.HasIndex(s => s.PostingNumber).IsUnique();

        builder.HasOne(s => s.MarketplaceClient)
            .WithMany()
            .HasForeignKey(s => s.MarketplaceClientId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(s => s.Status)
            .WithMany()
            .HasForeignKey(s => s.StatusId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasOne(s => s.DeliveryMethod)
            .WithMany()
            .HasForeignKey(s => s.DeliveryMethodId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasMany(s => s.Orders)
            .WithOne(o => o.Shipment)
            .HasForeignKey(o => o.ShipmentId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(s => s.Dates)
            .WithOne(d => d.Shipment)
            .HasForeignKey(d => d.ShipmentId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(s => s.Returns)
            .WithOne(r => r.Shipment)
            .HasForeignKey(r => r.ShipmentId)
            .OnDelete(DeleteBehavior.SetNull);
    }
}
