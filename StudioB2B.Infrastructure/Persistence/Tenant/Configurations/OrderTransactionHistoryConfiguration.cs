using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using StudioB2B.Domain.Entities.Orders;
using StudioB2B.Infrastructure.Persistence.Tenant;

namespace StudioB2B.Infrastructure.Persistence.Tenant.Configurations;

public class OrderTransactionHistoryConfiguration : IEntityTypeConfiguration<OrderTransactionHistory>
{
    public void Configure(EntityTypeBuilder<OrderTransactionHistory> builder)
    {
        builder.ToTable("OrderTransactionHistories");
        builder.HasKey(x => x.Id);

        builder.Property(x => x.PerformedByUserName).HasMaxLength(256);
        builder.Property(x => x.ErrorMessage).HasMaxLength(2000);

        builder.HasOne(x => x.Order)
            .WithMany()
            .HasForeignKey(x => x.OrderId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(x => x.OrderTransaction)
            .WithMany()
            .HasForeignKey(x => x.OrderTransactionId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne<ApplicationUser>()
            .WithMany()
            .HasForeignKey(x => x.PerformedByUserId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasIndex(x => x.PerformedAtUtc);
        builder.HasIndex(x => new { x.OrderId, x.OrderTransactionId });
    }
}
