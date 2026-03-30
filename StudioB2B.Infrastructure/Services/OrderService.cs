using Microsoft.EntityFrameworkCore;
using StudioB2B.Domain.Constants;
using StudioB2B.Domain.Entities;
using StudioB2B.Infrastructure.Features;
using StudioB2B.Infrastructure.Interfaces;
using StudioB2B.Shared;

namespace StudioB2B.Infrastructure.Services;

/// <summary>
/// Сервис заказов: инкапсулирует все запросы к БД страницы Orders
/// и автоматически применяет ограничения по правам пользователя.
/// </summary>
public class OrderService : IOrderService
{
    private readonly ITenantDbContextFactory _dbContextFactory;
    private readonly IEntityFilterService _entityFilterService;

    public OrderService(
        ITenantDbContextFactory dbContextFactory,
        IEntityFilterService entityFilterService)
    {
        _dbContextFactory = dbContextFactory;
        _entityFilterService = entityFilterService;
    }

    /// <inheritdoc/>
    public async Task<OrderInitData> GetInitDataAsync(CancellationToken ct = default)
    {
        var allowedClients = await _entityFilterService.GetAllowedIdsAsync(BlockedEntityTypeEnum.MarketplaceClient, ct);
        var allowedWarehouses = await _entityFilterService.GetAllowedIdsAsync(BlockedEntityTypeEnum.Warehouse, ct);
        var allowedStatuses = await _entityFilterService.GetAllowedIdsAsync(BlockedEntityTypeEnum.OrderStatus, ct);

        using var db = _dbContextFactory.CreateDbContext();
        return await db.GetOrderInitDataAsync(allowedClients, allowedWarehouses, allowedStatuses, ct);
    }

    /// <inheritdoc/>
    public async Task<OrderPageResult> GetOrderPageAsync(OrderPageRequest request, CancellationToken ct = default)
    {
        var allowedClients = await _entityFilterService.GetAllowedIdsAsync(BlockedEntityTypeEnum.MarketplaceClient, ct);
        var allowedWarehouses = await _entityFilterService.GetAllowedIdsAsync(BlockedEntityTypeEnum.Warehouse, ct);
        var allowedStatuses = await _entityFilterService.GetAllowedIdsAsync(BlockedEntityTypeEnum.OrderStatus, ct);
        var allowedDelivery = await _entityFilterService.GetAllowedIdsAsync(BlockedEntityTypeEnum.DeliveryMethod, ct);

        using var db = _dbContextFactory.CreateDbContext();
        return await db.GetOrderPageResultAsync(
            request, allowedClients, allowedWarehouses, allowedStatuses, allowedDelivery, ct);
    }

    /// <inheritdoc/>
    public async Task<Dictionary<Guid, string?>> GetTransactionColorsAsync(
        IEnumerable<Guid> orderIds, CancellationToken ct = default)
    {
        using var db = _dbContextFactory.CreateDbContext();
        return await db.GetOrderTransactionColorsAsync(orderIds, ct);
    }

    /// <inheritdoc/>
    public async Task<OrderSelectionInfo> GetSelectionInfoAsync(
        IEnumerable<Guid> orderIds, CancellationToken ct = default)
    {
        using var db = _dbContextFactory.CreateDbContext();
        return await db.GetOrderSelectionInfoAsync(orderIds, ct);
    }

    /// <inheritdoc/>
    public async Task<OrderEntity?> FindOrderByIdAsync(Guid id, CancellationToken ct = default)
    {
        using var db = _dbContextFactory.CreateDbContext();
        return await db.FindOrderByShipmentOrOrderIdAsync(id, ct);
    }

    /// <inheritdoc/>
    public async Task<OrderEntity?> GetOrderDetailAsync(Guid id, CancellationToken ct = default)
    {
        using var db = _dbContextFactory.CreateDbContext();
        return await db.Orders
            .IncludeForDetail()
            .AsNoTracking()
            .FirstOrDefaultAsync(o => o.Id == id, ct);
    }

    /// <inheritdoc/>
    public async Task<List<OrderEntity>> GetShipmentOrdersAsync(Guid shipmentId, CancellationToken ct = default)
    {
        using var db = _dbContextFactory.CreateDbContext();
        return await db.Orders
            .IncludeForShipmentList()
            .AsNoTracking()
            .Where(o => o.ShipmentId == shipmentId)
            .ToListAsync(ct);
    }
}

