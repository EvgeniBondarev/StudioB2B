using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using StudioB2B.Domain.Entities.Orders;

namespace StudioB2B.Infrastructure.Persistence.Tenant.Configurations;

public class WarehouseInfoConfiguration : IEntityTypeConfiguration<WarehouseInfo>
{
    public void Configure(EntityTypeBuilder<WarehouseInfo> builder)
    {
        builder.ToTable("WarehouseInfos");
        builder.HasKey(w => w.Id);

        // Два FK на таблицу Warehouses — нужны явные имена, иначе EF выберет Cascade для обоих
        builder.HasOne(w => w.RecipientWarehouse)
            .WithMany()
            .HasForeignKey(w => w.RecipientWarehouseId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasOne(w => w.SenderWarehouse)
            .WithMany()
            .HasForeignKey(w => w.SenderWarehouseId)
            .OnDelete(DeleteBehavior.SetNull);
    }
}
