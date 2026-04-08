using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using StudioB2B.Domain.Entities;

namespace StudioB2B.Infrastructure.Persistence.Tenant.Configurations;

public class TenantLoginCodeConfiguration : IEntityTypeConfiguration<TenantLoginCode>
{
    public void Configure(EntityTypeBuilder<TenantLoginCode> builder)
    {
        builder.ToTable("LoginCodes");

        builder.HasKey(e => e.Id);

        builder.Property(e => e.Code).IsRequired().HasMaxLength(10);

        builder.HasIndex(e => e.UserId);

        builder.HasOne(e => e.User)
            .WithMany()
            .HasForeignKey(e => e.UserId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}

