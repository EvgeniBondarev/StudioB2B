using Microsoft.EntityFrameworkCore;
using StudioB2B.Domain.Entities;
using StudioB2B.Infrastructure.Persistence.Tenant;

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
}

