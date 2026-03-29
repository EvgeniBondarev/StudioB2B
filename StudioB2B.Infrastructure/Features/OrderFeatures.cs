using System.Linq.Dynamic.Core;
using Microsoft.EntityFrameworkCore;
using StudioB2B.Domain.Entities;
using StudioB2B.Infrastructure.Persistence.Tenant;
using StudioB2B.Shared;

namespace StudioB2B.Infrastructure.Features;

public static class OrderExtensions
{
    // Order IQueryable includes
    public static IQueryable<OrderEntity> IncludeForGrid(this IQueryable<OrderEntity> q)
    {
        return q
            .Include(o => o.Shipment).ThenInclude(s => s.MarketplaceClient)
            .Include(o => o.Shipment).ThenInclude(s => s.DeliveryMethod).ThenInclude(dm => dm!.DeliveryType)
            .Include(o => o.Status)
            .Include(o => o.SystemStatus)
            .Include(o => o.ProductInfo).ThenInclude(pi => pi!.Product)
                .ThenInclude(p => p!.Manufacturer)
            .Include(o => o.WarehouseInfo).ThenInclude(wi => wi!.SenderWarehouse)
            .Include(o => o.Prices).ThenInclude(p => p.PriceType)
            .Include(o => o.Prices).ThenInclude(p => p.Currency);
    }

    public static IQueryable<OrderEntity> IncludeForDetail(this IQueryable<OrderEntity> q)
    {
        return q
            .Include(o => o.Shipment).ThenInclude(s => s.MarketplaceClient)
            .Include(o => o.Shipment).ThenInclude(s => s.Status)
            .Include(o => o.Shipment).ThenInclude(s => s.DeliveryMethod)
            .Include(o => o.Status)
            .Include(o => o.ProductInfo).ThenInclude(pi => pi!.Product)
                .ThenInclude(p => p!.Attributes).ThenInclude(a => a.Attribute)
            .Include(o => o.ProductInfo).ThenInclude(pi => pi!.Product)
                .ThenInclude(p => p!.Manufacturer)
            .Include(o => o.ProductInfo).ThenInclude(pi => pi!.Product)
                .ThenInclude(p => p!.Category)
            .Include(o => o.Recipient).ThenInclude(r => r!.Address)
            .Include(o => o.WarehouseInfo).ThenInclude(wi => wi!.SenderWarehouse)
            .Include(o => o.Prices).ThenInclude(p => p.PriceType)
            .Include(o => o.Prices).ThenInclude(p => p.Currency)
            .Include(o => o.Shipment).ThenInclude(s => s.Returns);
    }

    public static IQueryable<OrderEntity> IncludeForShipmentList(this IQueryable<OrderEntity> q)
    {
        return q
            .Include(o => o.Shipment)
            .Include(o => o.ProductInfo).ThenInclude(pi => pi!.Product)
            .Include(o => o.Status);
    }

    // OrderStatus methods (CRUD-like, TenantDbContext extensions)
    public static IQueryable<OrderStatus> IncludeEverything(this IQueryable<OrderStatus> q)
    {
        return q.Include(s => s.MarketplaceClientType);
    }

    public static IQueryable<OrderStatus> Active(this IQueryable<OrderStatus> q)
    {
        return q.Where(s => !s.IsDeleted);
    }

    public static async Task<OrderStatus> CreateOrderStatusAsync(this TenantDbContext db, OrderStatus status,
                                                                 CancellationToken ct = default)
    {
        db.OrderStatuses.Add(status);
        await db.SaveChangesAsync(ct);
        return status;
    }

    public static async Task UpdateOrderStatusAsync(this TenantDbContext db, OrderStatus status,
                                                    CancellationToken ct = default)
    {
        if (!db.OrderStatuses.Local.Contains(status))
        {
            db.OrderStatuses.Attach(status).State = EntityState.Modified;
        }

        await db.SaveChangesAsync(ct);
    }

    public static async Task SoftDeleteOrderStatusAsync(this TenantDbContext db, OrderStatus status,
                                                        CancellationToken ct = default)
    {
        status.IsDeleted = true;

        if (!db.OrderStatuses.Local.Contains(status))
        {
            db.OrderStatuses.Attach(status).State = EntityState.Modified;
        }

        await db.SaveChangesAsync(ct);
    }

    public static async Task<OrderStatusInitData> GetOrderStatusInitDataAsync(
        this TenantDbContext db,
        CancellationToken ct = default)
    {
        var clientTypes = await db.MarketplaceClientTypes!
            .AsNoTracking().OrderBy(t => t.Name).ToListAsync(ct);
        var statusData = await db.OrderStatuses
            .Active().AsNoTracking()
            .Select(s => new { s.IsInternal, s.IsTerminal, s.MarketplaceClientTypeId })
            .ToListAsync(ct);
        return new OrderStatusInitData(
            clientTypes,
            statusData.Count(s => s.IsInternal),
            statusData.Count(s => !s.IsInternal),
            statusData.Count(s => s.IsTerminal),
            statusData.Count(s => !s.IsTerminal),
            statusData.Where(s => s.MarketplaceClientTypeId.HasValue)
                      .GroupBy(s => s.MarketplaceClientTypeId!.Value)
                      .ToDictionary(g => g.Key, g => g.Count()));
    }

    public static async Task<(List<OrderStatus> Items, int TotalCount)> GetOrderStatusesPagedAsync(
        this TenantDbContext db,
        OrderStatusPageFilter filter,
        string? dynamicFilter,
        string? orderBy,
        int skip,
        int take,
        CancellationToken ct = default)
    {
        var query = db.OrderStatuses.IncludeEverything().Active().AsNoTracking().AsQueryable();
        if (filter.FilterType == "internal")
            query = query.Where(s => s.IsInternal);
        else if (filter.FilterType == "marketplace")
            query = query.Where(s => !s.IsInternal);
        if (filter.MarketplaceTypeId.HasValue)
            query = query.Where(s => s.MarketplaceClientTypeId == filter.MarketplaceTypeId.Value);
        if (filter.FilterTerminal.HasValue)
            query = query.Where(s => s.IsTerminal == filter.FilterTerminal.Value);
        if (!string.IsNullOrEmpty(dynamicFilter))
            query = query.Where(dynamicFilter);
        var total = await query.CountAsync(ct);
        query = !string.IsNullOrEmpty(orderBy)
            ? query.OrderBy(orderBy)
            : query.OrderBy(s => s.IsInternal).ThenBy(s => s.Name);
        return (await query.Skip(skip).Take(take).ToListAsync(ct), total);
    }

    public static async Task<OrderInitData> GetOrderInitDataAsync(
        this TenantDbContext db,
        ICollection<Guid>? allowedClients,
        ICollection<Guid>? allowedWarehouses,
        ICollection<Guid>? allowedStatuses,
        CancellationToken ct = default)
    {
        var clientsQ = db.MarketplaceClients!
            .Include(c => c.ClientType).Include(c => c.Mode).Include(c => c.Mode2)
            .Where(c => c.ClientType!.Name == "Ozon"
                        && ((c.Mode != null && (c.Mode.Name == "FBS" || c.Mode.Name == "FBO"))
                            || (c.Mode2 != null && (c.Mode2.Name == "FBS" || c.Mode2.Name == "FBO"))))
            .OrderBy(c => c.Name).AsNoTracking().AsQueryable();
        if (allowedClients is not null)
            clientsQ = clientsQ.Where(c => allowedClients.Contains(c.Id));

        var msStatusQ = db.OrderStatuses
            .Where(s => !s.IsInternal && !s.IsDeleted).OrderBy(s => s.Name).AsNoTracking().AsQueryable();
        if (allowedStatuses is not null)
            msStatusQ = msStatusQ.Where(s => allowedStatuses.Contains(s.Id));

        var sysStatusQ = db.OrderStatuses
            .Where(s => s.IsInternal && !s.IsDeleted).OrderBy(s => s.Name).AsNoTracking().AsQueryable();
        if (allowedStatuses is not null)
            sysStatusQ = sysStatusQ.Where(s => allowedStatuses.Contains(s.Id));

        var warehouseQ = db.Warehouses.OrderBy(w => w.Name).AsNoTracking().AsQueryable();
        if (allowedWarehouses is not null)
            warehouseQ = warehouseQ.Where(w => allowedWarehouses.Contains(w.Id));

        return new OrderInitData(
            await clientsQ.ToListAsync(ct),
            await msStatusQ.ToListAsync(ct),
            await sysStatusQ.ToListAsync(ct),
            await warehouseQ.ToListAsync(ct),
            await db.GetActiveRulesAsync(ct));
    }

    public static async Task<OrderPageResult> GetOrderPageResultAsync(
        this TenantDbContext db,
        OrderPageRequest request,
        ICollection<Guid>? allowedClients,
        ICollection<Guid>? allowedWarehouses,
        ICollection<Guid>? allowedStatuses,
        ICollection<Guid>? allowedDelivery,
        CancellationToken ct = default)
    {
        // Base query for pill-counts (no status / return filters)
        var countBase = db.Orders.AsNoTracking().AsQueryable();
        countBase = ApplyRestrictions(countBase, allowedClients, allowedWarehouses, allowedStatuses, allowedDelivery);
        if (request.ClientId.HasValue)
            countBase = countBase.Where(o => o.Shipment.MarketplaceClientId == request.ClientId.Value);
        if (request.WarehouseId.HasValue)
            countBase = countBase.Where(o => o.WarehouseInfo != null && o.WarehouseInfo.SenderWarehouseId == request.WarehouseId.Value);
        if (!string.IsNullOrWhiteSpace(request.SearchText))
            countBase = ApplySearch(countBase, request.SearchText);
        if (!string.IsNullOrEmpty(request.DynamicFilter))
            countBase = countBase.Where(request.DynamicFilter);

        var msCounts = await countBase
            .Where(o => o.StatusId != null)
            .GroupBy(o => o.StatusId!.Value)
            .Select(g => new { StatusId = g.Key, Count = g.Count() })
            .ToListAsync(ct);

        var ssCounts = await countBase
            .Where(o => o.SystemStatusId != null)
            .GroupBy(o => o.SystemStatusId!.Value)
            .Select(g => new { StatusId = g.Key, Count = g.Count() })
            .ToListAsync(ct);

        var returnCount = await countBase
            .Where(o => db.OrderReturns.Any(r => r.ShipmentId == o.ShipmentId))
            .Select(o => o.ShipmentId).Distinct().CountAsync(ct);

        var fbsCount = await countBase
            .Where(o => o.Shipment.DeliveryMethod!.DeliveryType != null && o.Shipment.DeliveryMethod.DeliveryType!.Name == "fbs")
            .CountAsync(ct);
        var fboCount = await countBase
            .Where(o => o.Shipment.DeliveryMethod!.DeliveryType != null && o.Shipment.DeliveryMethod.DeliveryType!.Name == "fbo")
            .CountAsync(ct);

        // Main query
        var query = db.Orders.IncludeForGrid().AsNoTracking().AsQueryable();
        query = ApplyRestrictions(query, allowedClients, allowedWarehouses, allowedStatuses, allowedDelivery);
        if (request.ClientId.HasValue)
            query = query.Where(o => o.Shipment.MarketplaceClientId == request.ClientId.Value);
        if (request.StatusId.HasValue)
            query = query.Where(o => o.StatusId == request.StatusId.Value);
        if (request.SystemStatusId.HasValue)
            query = query.Where(o => o.SystemStatusId == request.SystemStatusId.Value);
        if (request.WarehouseId.HasValue)
            query = query.Where(o => o.WarehouseInfo != null && o.WarehouseInfo.SenderWarehouseId == request.WarehouseId.Value);
        if (!string.IsNullOrWhiteSpace(request.SchemeType))
            query = query.Where(o => o.Shipment.DeliveryMethod!.DeliveryType != null && o.Shipment.DeliveryMethod.DeliveryType!.Name == request.SchemeType);
        if (request.HasReturn)
            query = query.Where(o => db.OrderReturns.Any(r => r.ShipmentId == o.ShipmentId));
        if (!string.IsNullOrWhiteSpace(request.SearchText))
            query = ApplySearch(query, request.SearchText);
        if (!string.IsNullOrEmpty(request.DynamicFilter))
            query = query.Where(request.DynamicFilter);

        List<OrderEntity> items;
        int totalCount;
        if (request.FetchAll)
        {
            items = await query.ToListAsync(ct);
            totalCount = items.Count;
        }
        else
        {
            totalCount = await query.CountAsync(ct);
            if (!string.IsNullOrEmpty(request.OrderBy))
                query = query.OrderBy(request.OrderBy);
            items = await query.Skip(request.Skip).Take(request.Take).ToListAsync(ct);
        }

        var transactionColors = await db.GetOrderTransactionColorsAsync(items.Select(o => o.Id), ct);

        return new OrderPageResult(
            items, totalCount,
            msCounts.ToDictionary(x => x.StatusId, x => x.Count),
            ssCounts.ToDictionary(x => x.StatusId, x => x.Count),
            returnCount,
            new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase) { ["fbs"] = fbsCount, ["fbo"] = fboCount },
            transactionColors);
    }

    public static async Task<Dictionary<Guid, string?>> GetOrderTransactionColorsAsync(
        this TenantDbContext db,
        IEnumerable<Guid> orderIds,
        CancellationToken ct = default)
    {
        var ids = orderIds.ToList();
        if (ids.Count == 0) return new Dictionary<Guid, string?>();
        var histories = await db.OrderTransactionHistories
            .Include(h => h.OrderTransaction)
            .Where(h => h.Success && ids.Contains(h.OrderId))
            .OrderByDescending(h => h.PerformedAtUtc)
            .ToListAsync(ct);
        return histories
            .GroupBy(h => h.OrderId)
            .ToDictionary(g => g.Key, g => g.First().OrderTransaction?.Color);
    }

    public static async Task<OrderSelectionInfo> GetOrderSelectionInfoAsync(
        this TenantDbContext db,
        IEnumerable<Guid> orderIds,
        CancellationToken ct = default)
    {
        var ids = orderIds.ToList();
        var statusIdsWithValue = await db.Orders
            .Where(o => ids.Contains(o.Id) && o.SystemStatusId != null)
            .Select(o => o.SystemStatusId!.Value)
            .Distinct()
            .ToListAsync(ct);
        var hasNull = await db.Orders
            .Where(o => ids.Contains(o.Id) && o.SystemStatusId == null)
            .AnyAsync(ct);
        var availableTransactions = new List<OrderTransaction>();
        if (statusIdsWithValue.Count == 1)
        {
            availableTransactions = await db.OrderTransactions
                .Include(t => t.FromSystemStatus)
                .Include(t => t.ToSystemStatus)
                .Where(t => !t.IsDeleted && t.IsEnabled && t.FromSystemStatusId == statusIdsWithValue[0])
                .OrderBy(t => t.Name)
                .AsNoTracking()
                .ToListAsync(ct);
        }
        return new OrderSelectionInfo(statusIdsWithValue, hasNull, availableTransactions);
    }

    public static async Task<OrderEntity?> FindOrderByShipmentOrOrderIdAsync(
        this TenantDbContext db,
        Guid id,
        CancellationToken ct = default)
    {
        return await db.Orders.AsNoTracking().FirstOrDefaultAsync(o => o.ShipmentId == id, ct)
               ?? await db.Orders.AsNoTracking().FirstOrDefaultAsync(o => o.Id == id, ct);
    }

    private static IQueryable<OrderEntity> ApplyRestrictions(
        IQueryable<OrderEntity> q,
        ICollection<Guid>? allowedClients,
        ICollection<Guid>? allowedWarehouses,
        ICollection<Guid>? allowedStatuses,
        ICollection<Guid>? allowedDelivery)
    {
        if (allowedClients is not null)
            q = q.Where(o => allowedClients.Contains(o.Shipment.MarketplaceClientId));
        if (allowedWarehouses is not null)
            q = q.Where(o => o.WarehouseInfo != null && o.WarehouseInfo.SenderWarehouseId.HasValue && allowedWarehouses.Contains(o.WarehouseInfo.SenderWarehouseId.Value));
        if (allowedStatuses is not null)
            q = q.Where(o => o.StatusId != null && allowedStatuses.Contains(o.StatusId.Value));
        if (allowedDelivery is not null)
            q = q.Where(o => o.Shipment.DeliveryMethodId != null && allowedDelivery.Contains(o.Shipment.DeliveryMethodId.Value));
        return q;
    }

    private static IQueryable<OrderEntity> ApplySearch(IQueryable<OrderEntity> q, string searchText)
    {
        var term = searchText.Trim();
        return q.Where(o =>
            o.Shipment.PostingNumber.Contains(term)
            || (o.ProductInfo != null && o.ProductInfo.Product != null && o.ProductInfo.Product.Article != null && o.ProductInfo.Product.Article.Contains(term))
            || (o.ProductInfo != null && o.ProductInfo.Product != null && o.ProductInfo.Product.Name.Contains(term))
            || (o.ProductInfo != null && o.ProductInfo.Product != null && o.ProductInfo.Product.Manufacturer != null && o.ProductInfo.Product.Manufacturer.Name.Contains(term)));
    }
}
