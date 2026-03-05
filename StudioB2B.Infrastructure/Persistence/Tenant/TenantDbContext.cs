using System.Collections.Concurrent;
using System.Linq.Expressions;
using System.Reflection;
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
    /// <summary>Поля Identity и системные поля, которые не нужно аудировать.</summary>
    private static readonly HashSet<string> ExcludedProperties = new(StringComparer.OrdinalIgnoreCase)
    {
        "PasswordHash",
        "SecurityStamp",
        "ConcurrencyStamp",
        "NormalizedUserName",
        "NormalizedEmail",
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

    // ── Background Jobs ──────────────────────────────────────────────
    public DbSet<SyncJobHistory> SyncJobHistories { get; set; } = null!;

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
    /// Каждый пакет сохраняется в отдельной мини-транзакции, поэтому не влияет на основные данные.
    /// После успешного сброса буфер очищается.
    /// </summary>
    public async Task FlushDeferredAuditAsync(int batchSize = 200, CancellationToken ct = default)
    {
        if (_deferredAuditBuffer.Count == 0)
            return;

        for (var i = 0; i < _deferredAuditBuffer.Count; i += batchSize)
        {
            var batch = _deferredAuditBuffer.GetRange(i, Math.Min(batchSize, _deferredAuditBuffer.Count - i));
            FieldAuditLogs.AddRange(batch);

            // Сохраняем вне любой внешней транзакции: используем SuppressAudit чтобы
            // не попасть в рекурсию (FieldAuditLog сам не является IBaseEntity).
            await base.SaveChangesAsync(ct);
        }

        _deferredAuditBuffer.Clear();
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
            if (entry.State is not (EntityState.Modified or EntityState.Deleted))
                continue;

            var entityName = entry.Metadata.ShortName();
            var entityId   = entry.Properties
                .FirstOrDefault(p => p.Metadata.IsPrimaryKey())?.CurrentValue?.ToString() ?? string.Empty;
            var changeType = entry.State.ToString(); // "Added" / "Modified" / "Deleted"
            var changedAt  = DateTime.UtcNow;

            var clrType   = entry.Entity.GetType();
            var skipProps = _skipAuditCache.GetOrAdd(clrType, t =>
            {
                var set = new HashSet<string>(StringComparer.Ordinal);

                // Свойства самого класса с [SkipAudit]
                foreach (var p in t.GetProperties(BindingFlags.Public | BindingFlags.Instance))
                {
                    if (p.GetCustomAttribute<SkipAuditAttribute>() != null)
                        set.Add(p.Name);
                }

                // Свойства интерфейсов с [SkipAudit] (например ISoftDelete.IsDeleted)
                foreach (var iface in t.GetInterfaces())
                {
                    foreach (var p in iface.GetProperties())
                    {
                        if (p.GetCustomAttribute<SkipAuditAttribute>() != null)
                            set.Add(p.Name);
                    }
                }

                return set;
            });

            foreach (PropertyEntry prop in entry.Properties)
            {
                if (ExcludedProperties.Contains(prop.Metadata.Name))
                    continue;

                if (skipProps.Contains(prop.Metadata.Name))
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

                // Пропускаем «заполнение» пустых полей системой (sync) — по сути как создание значения
                if (entry.State == EntityState.Modified && oldValue == null && userId == SystemUser.RobotId)
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
