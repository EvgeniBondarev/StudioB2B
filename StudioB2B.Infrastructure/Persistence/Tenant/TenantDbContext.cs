using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using StudioB2B.Domain.Entities.Common;
using StudioB2B.Domain.Entities.Marketplace;

namespace StudioB2B.Infrastructure.Persistence.Tenant;

/// <summary>
/// Tenant Database Context - база данных конкретного тенанта
/// Содержит: Users, Orders, Transactions, Warehouses и т.д.
/// </summary>
public class TenantDbContext : IdentityDbContext<ApplicationUser, ApplicationRole, Guid>
{
    private readonly Guid? _currentUserId;

    public TenantDbContext(DbContextOptions<TenantDbContext> options) : base(options)
    {
    }

    public TenantDbContext(DbContextOptions<TenantDbContext> options, Guid? currentUserId)
        : base(options)
    {
        _currentUserId = currentUserId;
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Применяем все конфигурации из папки Tenant
        modelBuilder.ApplyConfigurationsFromAssembly(
            typeof(TenantDbContext).Assembly,
            type => type.Namespace?.Contains("Tenant") == true ||
                    type.Namespace?.Contains("Configurations") == true);

        // Global Query Filters для Soft Delete
        foreach (var entityType in modelBuilder.Model.GetEntityTypes())
        {
            if (typeof(ISoftDelete).IsAssignableFrom(entityType.ClrType))
            {
                var parameter = System.Linq.Expressions.Expression.Parameter(entityType.ClrType, "e");
                var property = System.Linq.Expressions.Expression.Property(parameter, nameof(ISoftDelete.IsDeleted));
                var falseConstant = System.Linq.Expressions.Expression.Constant(false);
                var lambda = System.Linq.Expressions.Expression.Lambda(
                    System.Linq.Expressions.Expression.Equal(property, falseConstant),
                    parameter);

                modelBuilder.Entity(entityType.ClrType).HasQueryFilter(lambda);
            }
        }
    }

    public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        UpdateAuditFields();
        return base.SaveChangesAsync(cancellationToken);
    }

    public DbSet<MarketplaceClient>? MarketplaceClients { get; set; }
    public DbSet<MarketplaceClientType>? MarketplaceClientTypes { get; set; }
    public DbSet<MarketplaceClientMode>? MarketplaceClientModes { get; set; }
    public DbSet<MarketplaceClientSettings>? MarketplaceClientSettings { get; set; }
    public DbSet<MarketplaceClient1CSettings>? MarketplaceClient1CSettings { get; set; }

    public override int SaveChanges()
    {
        UpdateAuditFields();
        return base.SaveChanges();
    }

    private void UpdateAuditFields()
    {
        var entries = ChangeTracker.Entries<IAuditableEntity>();

        foreach (var entry in entries)
        {
            if (entry.State == EntityState.Added)
            {
                if (_currentUserId.HasValue)
                {
                    entry.Entity.GetType().GetMethod("SetCreatedBy")?.Invoke(entry.Entity, [_currentUserId.Value]);
                }
            }

            if (entry.State == EntityState.Modified)
            {
                if (_currentUserId.HasValue)
                {
                    entry.Entity.GetType().GetMethod("SetModified")?.Invoke(entry.Entity, [_currentUserId.Value]);
                }
            }
        }
    }
}
