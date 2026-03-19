using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using StudioB2B.Domain.Entities;

namespace StudioB2B.Infrastructure.Persistence.Tenant.Configurations;

public class OrderConfiguration : IEntityTypeConfiguration<OrderEntity>
{
    public void Configure(EntityTypeBuilder<OrderEntity> builder)
    {
        builder.ToTable("Orders");
        builder.HasKey(o => o.Id);

        // Два FK на один и тот же тип OrderStatus — явно задаём имена
        builder.HasOne(o => o.Status)
            .WithMany()
            .HasForeignKey(o => o.StatusId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasOne(o => o.SystemStatus)
            .WithMany()
            .HasForeignKey(o => o.SystemStatusId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasOne(o => o.ProductInfo)
            .WithOne(p => p.OrderEntity)
            .HasForeignKey<OrderProductInfo>(p => p.OrderId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(o => o.Recipient)
            .WithMany()
            .HasForeignKey(o => o.RecipientId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasOne(o => o.WarehouseInfo)
            .WithMany()
            .HasForeignKey(o => o.WarehouseInfoId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasMany(o => o.Prices)
            .WithOne(p => p.OrderEntity)
            .HasForeignKey(p => p.OrderId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
