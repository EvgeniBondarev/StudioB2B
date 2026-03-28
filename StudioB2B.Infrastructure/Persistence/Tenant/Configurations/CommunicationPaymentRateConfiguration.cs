using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using StudioB2B.Domain.Entities;

namespace StudioB2B.Infrastructure.Persistence.Tenant.Configurations;

public class CommunicationPaymentRateConfiguration : IEntityTypeConfiguration<CommunicationPaymentRate>
{
    public void Configure(EntityTypeBuilder<CommunicationPaymentRate> builder)
    {
        builder.ToTable("CommunicationPaymentRates");
        builder.HasKey(x => x.Id);

        builder.Property(x => x.TaskType).IsRequired(false);
        builder.Property(x => x.PaymentMode).IsRequired();
        builder.Property(x => x.Rate).HasColumnType("decimal(18,2)").IsRequired();
        builder.Property(x => x.Description).HasMaxLength(256);

        builder.HasOne(x => x.User)
            .WithMany()
            .HasForeignKey(x => x.UserId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasIndex(x => new { x.TaskType, x.IsActive });
        builder.HasIndex(x => x.UserId);
    }
}
