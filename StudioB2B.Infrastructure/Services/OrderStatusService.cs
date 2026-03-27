using StudioB2B.Domain.Entities;
using StudioB2B.Infrastructure.Features;
using StudioB2B.Infrastructure.Interfaces;
using StudioB2B.Shared;

namespace StudioB2B.Infrastructure.Services;

/// <summary>
/// Сервис статусов заказов: инкапсулирует все запросы к БД,
/// использует extension-методы из OrderFeatures.
/// </summary>
public class OrderStatusService : IOrderStatusService
{
    private readonly ITenantDbContextFactory _dbContextFactory;

    public OrderStatusService(ITenantDbContextFactory dbContextFactory)
    {
        _dbContextFactory = dbContextFactory;
    }

    /// <inheritdoc/>
    public async Task<OrderStatusInitData> GetInitDataAsync(CancellationToken ct = default)
    {
        using var db = _dbContextFactory.CreateDbContext();
        return await db.GetOrderStatusInitDataAsync(ct);
    }

    /// <inheritdoc/>
    public async Task<(List<OrderStatus> Items, int TotalCount)> GetPagedAsync(
        OrderStatusPageFilter filter,
        string?               dynamicFilter,
        string?               orderBy,
        int                   skip,
        int                   take,
        CancellationToken     ct = default)
    {
        using var db = _dbContextFactory.CreateDbContext();
        return await db.GetOrderStatusesPagedAsync(filter, dynamicFilter, orderBy, skip, take, ct);
    }

    /// <inheritdoc/>
    public async Task<OrderStatus> CreateAsync(OrderStatus status, CancellationToken ct = default)
    {
        using var db = _dbContextFactory.CreateDbContext();
        return await db.CreateOrderStatusAsync(status, ct);
    }

    /// <inheritdoc/>
    public async Task UpdateAsync(OrderStatus status, CancellationToken ct = default)
    {
        using var db = _dbContextFactory.CreateDbContext();
        await db.UpdateOrderStatusAsync(status, ct);
    }

    /// <inheritdoc/>
    public async Task SoftDeleteAsync(OrderStatus status, CancellationToken ct = default)
    {
        using var db = _dbContextFactory.CreateDbContext();
        await db.SoftDeleteOrderStatusAsync(status, ct);
    }
}

