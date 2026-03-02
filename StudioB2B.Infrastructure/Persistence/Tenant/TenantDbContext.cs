using System.Linq.Expressions;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;
using StudioB2B.Domain.Entities.Common;
using StudioB2B.Domain.Entities.Marketplace;
using StudioB2B.Domain.Entities.Orders;
using StudioB2B.Domain.Entities.Products;
using StudioB2B.Domain.Entities.References;
using StudioB2B.Domain.Entities.Warehouses;

namespace StudioB2B.Infrastructure.Persistence.Tenant;

public class TenantDbContext : IdentityDbContext<ApplicationUser, ApplicationRole, Guid>
{
    // ── Marketplace ──────────────────────────────────────────────────
    public DbSet<MarketplaceClient>? MarketplaceClients { get; set; }
    public DbSet<MarketplaceClientType>? MarketplaceClientTypes { get; set; }
    public DbSet<MarketplaceClientMode>? MarketplaceClientModes { get; set; }
    public DbSet<MarketplaceClientSettings>? MarketplaceClientSettings { get; set; }
    public DbSet<MarketplaceClient1CSettings>? MarketplaceClient1CSettings { get; set; }

    // ── Orders ───────────────────────────────────────────────────────
    public DbSet<OrderStatus> OrderStatuses { get; set; } = null!;
    public DbSet<CalculationRule> CalculationRules { get; set; } = null!;
    public DbSet<StatusColor> StatusColors { get; set; } = null!;
    public DbSet<DateType> DateTypes { get; set; } = null!;
    public DbSet<DeliveryType> DeliveryTypes { get; set; } = null!;
    public DbSet<DeliveryMethod> DeliveryMethods { get; set; } = null!;
    public DbSet<Address> Addresses { get; set; } = null!;
    public DbSet<Recipient> Recipients { get; set; } = null!;
    public DbSet<WarehouseInfo> WarehouseInfos { get; set; } = null!;
    public DbSet<Shipment> Shipments { get; set; } = null!;
    public DbSet<ShipmentDate> ShipmentDates { get; set; } = null!;
    public DbSet<Order> Orders { get; set; } = null!;
    public DbSet<OrderPrice> OrderPrices { get; set; } = null!;
    public DbSet<OrderProductInfo> OrderProductInfos { get; set; } = null!;

    // ── Products ─────────────────────────────────────────────────────
    public DbSet<Category> Categories { get; set; } = null!;
    public DbSet<Manufacturer> Manufacturers { get; set; } = null!;
    public DbSet<Supplier> Suppliers { get; set; } = null!;
    public DbSet<Product> Products { get; set; } = null!;
    public DbSet<ProductAttribute> ProductAttributes { get; set; } = null!;
    public DbSet<ProductAttributeValue> ProductAttributeValues { get; set; } = null!;

    // ── References ───────────────────────────────────────────────────
    public DbSet<Currency> Currencies { get; set; } = null!;
    public DbSet<PriceType> PriceTypes { get; set; } = null!;

    // ── Warehouses ───────────────────────────────────────────────────
    public DbSet<Warehouse> Warehouses { get; set; } = null!;
    public DbSet<WarehouseStock> WarehouseStocks { get; set; } = null!;

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
