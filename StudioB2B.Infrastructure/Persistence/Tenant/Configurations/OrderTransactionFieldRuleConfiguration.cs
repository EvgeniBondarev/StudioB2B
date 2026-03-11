using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using StudioB2B.Domain.Entities.Orders;

namespace StudioB2B.Infrastructure.Persistence.Tenant.Configurations;

public class OrderTransactionFieldRuleConfiguration : IEntityTypeConfiguration<OrderTransactionFieldRule>
{
    public void Configure(EntityTypeBuilder<OrderTransactionFieldRule> builder)
    {
        builder.ToTable("OrderTransactionFieldRules");
        builder.HasKey(r => r.Id);

        builder.Property(r => r.EntityPath).IsRequired().HasMaxLength(100);

        builder.HasOne(r => r.OrderTransaction)
            .WithMany(t => t.FieldRules)
            .HasForeignKey(r => r.OrderTransactionId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
