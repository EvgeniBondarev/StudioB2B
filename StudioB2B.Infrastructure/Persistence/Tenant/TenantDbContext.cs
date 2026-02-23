using System.Linq.Expressions;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;
using StudioB2B.Domain.Entities.Common;
using StudioB2B.Domain.Entities.Marketplace;

namespace StudioB2B.Infrastructure.Persistence.Tenant;


public class TenantDbContext : IdentityDbContext<ApplicationUser, ApplicationRole, Guid>
{
    public DbSet<MarketplaceClient>? MarketplaceClients { get; set; }
    public DbSet<MarketplaceClientType>? MarketplaceClientTypes { get; set; }
    public DbSet<MarketplaceClientMode>? MarketplaceClientModes { get; set; }
    public DbSet<MarketplaceClientSettings>? MarketplaceClientSettings { get; set; }
    public DbSet<MarketplaceClient1CSettings>? MarketplaceClient1CSettings { get; set; }

    public TenantDbContext(DbContextOptions<TenantDbContext> options) : base(options)
    {
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.ApplyConfigurationsFromAssembly(
            typeof(TenantDbContext).Assembly,
            type => type.Namespace?.Contains("Tenant") == true ||
                    type.Namespace?.Contains("Configurations") == true);

        ApplySoftDeleteFilters(modelBuilder);
    }

    private static void ApplySoftDeleteFilters(ModelBuilder modelBuilder)
    {
        foreach (IMutableEntityType entityType in modelBuilder.Model.GetEntityTypes())
        {
            if (!typeof(ISoftDelete).IsAssignableFrom(entityType.ClrType))
                continue;

            ParameterExpression param = Expression.Parameter(entityType.ClrType, "e");
            MemberExpression isDeletedProp = Expression.Property(param, nameof(ISoftDelete.IsDeleted));
            UnaryExpression notDeleted = Expression.Not(isDeletedProp);
            LambdaExpression lambda = Expression.Lambda(notDeleted, param);

            entityType.SetQueryFilter(lambda);
        }
    }
}
