using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using StudioB2B.Domain.Entities;

namespace StudioB2B.Infrastructure.Persistence.Tenant.Configurations;

public class TenantUserActivationTokenConfiguration : IEntityTypeConfiguration<TenantUserActivationToken>
{
    public void Configure(EntityTypeBuilder<TenantUserActivationToken> builder)
    {
        builder.ToTable("UserActivationTokens");

        builder.HasKey(e => e.Id);

        builder.HasIndex(e => e.UserId);

        builder.HasOne(e => e.User)
            .WithMany()
            .HasForeignKey(e => e.UserId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}

