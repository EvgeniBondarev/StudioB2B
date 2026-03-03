using System.Linq.Expressions;
using System.Text.Json;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Metadata;
using StudioB2B.Application.Common.Interfaces;
using StudioB2B.Domain.Entities.Common;
using StudioB2B.Domain.Entities.Marketplace;
using StudioB2B.Domain.Entities.Orders;
using StudioB2B.Domain.Entities.Products;
using StudioB2B.Domain.Entities.References;
using StudioB2B.Domain.Entities.Warehouses;

namespace StudioB2B.Infrastructure.Persistence.Tenant;

public class TenantDbContext : IdentityDbContext<ApplicationUser, ApplicationRole, Guid>
{
    /// <summary>Поля Identity, которые не нужно аудировать.</summary>
    private static readonly HashSet<string> ExcludedProperties = new(StringComparer.OrdinalIgnoreCase)
    {
        "PasswordHash",
        "SecurityStamp",
        "ConcurrencyStamp",
        "NormalizedUserName",
        "NormalizedEmail"
    };

    private readonly ICurrentUserProvider? _currentUserProvider;

    /// <summary>
    /// Когда <c>true</c> — аудит изменений не записывается.
    /// Используется в фоновых операциях массового импорта (синхронизация заказов),
    /// чтобы не генерировать тысячи INSERT в FieldAuditLogs за одну транзакцию.
    /// </summary>
    public bool SuppressAudit { get; set; }

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

    // ── Audit ────────────────────────────────────────────────────────
    public DbSet<FieldAuditLog> FieldAuditLogs { get; set; } = null!;

    public TenantDbContext(
        DbContextOptions<TenantDbContext> options,
        ICurrentUserProvider? currentUserProvider = null) : base(options)
    {
        _currentUserProvider = currentUserProvider;
    }

    public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        if (!SuppressAudit)
        {
            var auditLogs = BuildAuditLogs();
            if (auditLogs.Count > 0)
                FieldAuditLogs.AddRange(auditLogs);
        }

        return await base.SaveChangesAsync(cancellationToken);
    }

    private List<FieldAuditLog> BuildAuditLogs()
    {
        var userId   = _currentUserProvider?.UserId   ?? SystemUser.RobotId;
        var userName = _currentUserProvider?.IsAuthenticated == true
            ? _currentUserProvider.Email
            : SystemUser.RobotEmail;

        var logs = new List<FieldAuditLog>();

        foreach (EntityEntry entry in ChangeTracker.Entries<IBaseEntity>())
        {
            if (entry.State is not (EntityState.Added or EntityState.Modified or EntityState.Deleted))
                continue;

            var entityName = entry.Metadata.ShortName();
            var entityId   = entry.Properties
                .FirstOrDefault(p => p.Metadata.IsPrimaryKey())?.CurrentValue?.ToString() ?? string.Empty;
            var changeType = entry.State.ToString(); // "Added" / "Modified" / "Deleted"
            var changedAt  = DateTime.UtcNow;

            foreach (PropertyEntry prop in entry.Properties)
            {
                if (ExcludedProperties.Contains(prop.Metadata.Name))
                    continue;

                // Пропускаем неизменённые поля при Modified
                if (entry.State == EntityState.Modified && !prop.IsModified)
                    continue;

                var oldValue = entry.State == EntityState.Modified || entry.State == EntityState.Deleted
                    ? Serialize(prop.OriginalValue)
                    : null;

                var newValue = entry.State == EntityState.Deleted
                    ? null
                    : Serialize(prop.CurrentValue);

                // Не пишем запись если оба значения одинаковы (актуально для Added с дефолтами)
                if (entry.State == EntityState.Added && oldValue == newValue)
                    continue;

                logs.Add(new FieldAuditLog
                {
                    EntityName        = entityName,
                    EntityId          = entityId,
                    FieldName         = prop.Metadata.Name,
                    OldValue          = oldValue,
                    NewValue          = newValue,
                    ChangedByUserId   = userId,
                    ChangedByUserName = userName,
                    ChangedAtUtc      = changedAt,
                    ChangeType        = changeType
                });
            }
        }

        return logs;
    }

    private static string? Serialize(object? value)
    {
        if (value is null) return null;
        return JsonSerializer.Serialize(value);
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
