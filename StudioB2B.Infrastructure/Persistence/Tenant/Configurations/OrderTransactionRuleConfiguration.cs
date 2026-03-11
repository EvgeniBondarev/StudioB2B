using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using StudioB2B.Domain.Entities;

namespace StudioB2B.Infrastructure.Persistence.Tenant.Configurations;

public class OrderTransactionRuleConfiguration : IEntityTypeConfiguration<OrderTransactionRule>
{
    public void Configure(EntityTypeBuilder<OrderTransactionRule> builder)
    {
        builder.ToTable("OrderTransactionRules");
        builder.HasKey(r => r.Id);

        builder.HasOne(r => r.PriceType)
            .WithMany()
            .HasForeignKey(r => r.PriceTypeId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(r => r.Product)
            .WithMany()
            .HasForeignKey(r => r.ProductId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(r => r.Currency)
            .WithMany()
            .HasForeignKey(r => r.CurrencyId)
            .OnDelete(DeleteBehavior.SetNull);
    }
}
