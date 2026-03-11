using System.Collections.Concurrent;
using System.Linq.Expressions;
using System.Reflection;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using StudioB2B.Domain.Entities;
using StudioB2B.Infrastructure.Interfaces;

namespace StudioB2B.Infrastructure.Persistence.Tenant;

public class TenantDbContext : DbContext
{
    /// <summary>Поля, которые не нужно аудировать.</summary>
    private static readonly HashSet<string> ExcludedProperties = new(StringComparer.OrdinalIgnoreCase)
    {
        nameof(TenantUser.HashPassword),
        nameof(ISoftDelete.IsDeleted)
    };

    /// <summary>Кэш: CLR-тип → набор имён свойств, помеченных [SkipAudit].</summary>
    private static readonly ConcurrentDictionary<Type, HashSet<string>> _skipAuditCache = new();

    private readonly ICurrentUserProvider? _currentUserProvider;

    /// <summary>
    /// Когда <c>true</c> — аудит изменений не записывается совсем.
    /// </summary>
    public bool SuppressAudit { get; set; }

    /// <summary>
    /// Когда <c>true</c> — аудит-записи накапливаются в <see cref="_deferredAuditBuffer"/>
    /// и НЕ вставляются в БД внутри текущей транзакции.
    /// Вызовите <see cref="FlushDeferredAuditAsync"/> чтобы сбросить буфер отдельными пакетами.
    /// </summary>
    public bool DeferAudit { get; set; }

    private readonly List<FieldAuditLog> _deferredAuditBuffer = [];

    #region DataSets

    public DbSet<TenantUser> Users { get; set; } = null!;

    public DbSet<TenantRole> Roles { get; set; } = null!;

    public DbSet<TenantUserRole> UserRoles { get; set; } = null!;

    public DbSet<MarketplaceClient>? MarketplaceClients { get; set; }

    public DbSet<MarketplaceClientType>? MarketplaceClientTypes { get; set; }

    public DbSet<MarketplaceClientMode>? MarketplaceClientModes { get; set; }

    public DbSet<MarketplaceClientSettings>? MarketplaceClientSettings { get; set; }

    public DbSet<MarketplaceClient1CSettings>? MarketplaceClient1CSettings { get; set; }

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

    public DbSet<OrderTransaction> OrderTransactions { get; set; } = null!;

    public DbSet<OrderTransactionRule> OrderTransactionRules { get; set; } = null!;

    public DbSet<OrderTransactionFieldRule> OrderTransactionFieldRules { get; set; } = null!;

    public DbSet<OrderTransactionHistory> OrderTransactionHistories { get; set; } = null!;

    public DbSet<Category> Categories { get; set; } = null!;

    public DbSet<Manufacturer> Manufacturers { get; set; } = null!;

    public DbSet<Supplier> Suppliers { get; set; } = null!;

    public DbSet<Product> Products { get; set; } = null!;

    public DbSet<ProductAttribute> ProductAttributes { get; set; } = null!;

    public DbSet<ProductAttributeValue> ProductAttributeValues { get; set; } = null!;

    public DbSet<Currency> Currencies { get; set; } = null!;

    public DbSet<PriceType> PriceTypes { get; set; } = null!;

    public DbSet<Warehouse> Warehouses { get; set; } = null!;

    public DbSet<WarehouseStock> WarehouseStocks { get; set; } = null!;

    public DbSet<FieldAuditLog> FieldAuditLogs { get; set; } = null!;

    public DbSet<SyncJobHistory> SyncJobHistories { get; set; } = null!;

    public DbSet<SyncJobSchedule> SyncJobSchedules { get; set; } = null!;

    #endregion

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
            {
                if (DeferAudit)
                    _deferredAuditBuffer.AddRange(auditLogs);
                else
                    FieldAuditLogs.AddRange(auditLogs);
            }
        }

        return await base.SaveChangesAsync(cancellationToken);
    }

    /// <summary>
    /// Сбрасывает накопленный буфер аудита в БД пакетами по <paramref name="batchSize"/> записей.
    /// </summary>
    public async Task FlushDeferredAuditAsync(int batchSize = 200, CancellationToken ct = default)
    {
        if (_deferredAuditBuffer.Count == 0)
            return;

        for (var i = 0; i < _deferredAuditBuffer.Count; i += batchSize)
        {
            var batch = _deferredAuditBuffer.GetRange(i, Math.Min(batchSize, _deferredAuditBuffer.Count - i));
            FieldAuditLogs.AddRange(batch);
            await base.SaveChangesAsync(ct);
        }

        _deferredAuditBuffer.Clear();
    }

    private List<FieldAuditLog> BuildAuditLogs()
    {
        var userId = _currentUserProvider?.UserId ?? SystemUser.RobotId;
        var userName = _currentUserProvider?.IsAuthenticated == true
            ? _currentUserProvider.Email
            : SystemUser.RobotEmail;

        var logs = new List<FieldAuditLog>();

        foreach (EntityEntry entry in ChangeTracker.Entries<IBaseEntity>())
        {
            if (entry.State is not (EntityState.Modified or EntityState.Deleted))
                continue;

            var entityName = entry.Metadata.ShortName();
            var entityId = entry.Properties
                .FirstOrDefault(p => p.Metadata.IsPrimaryKey())?.CurrentValue?.ToString() ?? string.Empty;
            var changeType = entry.State.ToString();
            var changedAt = DateTime.UtcNow;

            var clrType = entry.Entity.GetType();
            var skipProps = _skipAuditCache.GetOrAdd(clrType, t =>
            {
                var set = new HashSet<string>(StringComparer.Ordinal);
                foreach (var p in t.GetProperties(BindingFlags.Public | BindingFlags.Instance))
                    if (p.GetCustomAttribute<SkipAuditAttribute>() != null)
                        set.Add(p.Name);
                foreach (var iface in t.GetInterfaces())
                    foreach (var p in iface.GetProperties())
                        if (p.GetCustomAttribute<SkipAuditAttribute>() != null)
                            set.Add(p.Name);
                return set;
            });

            foreach (var prop in entry.Properties)
            {
                if (ExcludedProperties.Contains(prop.Metadata.Name))
                    continue;
                if (skipProps.Contains(prop.Metadata.Name))
                    continue;
                if (entry.State == EntityState.Modified && !prop.IsModified)
                    continue;

                var oldValue = entry.State == EntityState.Modified || entry.State == EntityState.Deleted
                    ? Serialize(prop.OriginalValue)
                    : null;
                var newValue = entry.State == EntityState.Deleted
                    ? null
                    : Serialize(prop.CurrentValue);

                if (entry.State == EntityState.Added && oldValue == newValue)
                    continue;
                if (entry.State == EntityState.Modified && oldValue == null && userId == SystemUser.RobotId)
                    continue;

                logs.Add(new FieldAuditLog
                {
                    EntityName = entityName,
                    EntityId = entityId,
                    FieldName = prop.Metadata.Name,
                    OldValue = oldValue,
                    NewValue = newValue,
                    ChangedByUserId = userId,
                    ChangedByUserName = userName,
                    ChangedAtUtc = changedAt,
                    ChangeType = changeType
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
        foreach (var entityType in modelBuilder.Model.GetEntityTypes())
        {
            if (!typeof(ISoftDelete).IsAssignableFrom(entityType.ClrType))
                continue;

            var param = Expression.Parameter(entityType.ClrType, "e");
            var isDeletedProp = Expression.Property(param, nameof(ISoftDelete.IsDeleted));
            var notDeleted = Expression.Not(isDeletedProp);
            var lambda = Expression.Lambda(notDeleted, param);

            entityType.SetQueryFilter(lambda);
        }
    }
}
