using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using StudioB2B.Domain.Entities;

namespace StudioB2B.Infrastructure.Persistence.Tenant.Configurations;

public class OrderTransactionConfiguration : IEntityTypeConfiguration<OrderTransaction>
{
    public void Configure(EntityTypeBuilder<OrderTransaction> builder)
    {
        builder.ToTable("OrderTransactions");
        builder.HasKey(t => t.Id);

        builder.Property(t => t.Name).IsRequired().HasMaxLength(200);
        builder.Property(t => t.Icon).HasMaxLength(100);

        builder.HasOne(t => t.FromSystemStatus)
            .WithMany()
            .HasForeignKey(t => t.FromSystemStatusId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(t => t.ToSystemStatus)
            .WithMany()
            .HasForeignKey(t => t.ToSystemStatusId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasMany(t => t.Rules)
            .WithOne(r => r.OrderTransaction)
            .HasForeignKey(r => r.OrderTransactionId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(t => t.FieldRules)
            .WithOne(r => r.OrderTransaction)
            .HasForeignKey(r => r.OrderTransactionId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
