using Microsoft.EntityFrameworkCore;
using StudioB2B.Domain.Entities;

namespace StudioB2B.Infrastructure.Features.Orders;

public static class OrderExtensions
{
    /// <summary>
    /// Базовый набор Include для списка заказов (страница Orders).
    /// </summary>
    public static IQueryable<Order> IncludeForGrid(this IQueryable<Order> q)
    {
        return q
            .Include(o => o.Shipment).ThenInclude(s => s.MarketplaceClient)
            .Include(o => o.Shipment).ThenInclude(s => s.DeliveryMethod)
            .Include(o => o.Status)
            .Include(o => o.SystemStatus)
            .Include(o => o.ProductInfo).ThenInclude(pi => pi!.Product)
            .Include(o => o.WarehouseInfo).ThenInclude(wi => wi!.SenderWarehouse)
            .Include(o => o.Prices).ThenInclude(p => p.PriceType)
            .Include(o => o.Prices).ThenInclude(p => p.Currency);
    }

    /// <summary>
    /// Полный набор Include для детального просмотра заказа (OrderDetailDialog).
    /// </summary>
    public static IQueryable<Order> IncludeForDetail(this IQueryable<Order> q)
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

    /// <summary>
    /// Набор Include для списка позиций внутри одного отправления.
    /// </summary>
    public static IQueryable<Order> IncludeForShipmentList(this IQueryable<Order> q)
    {
        return q
            .Include(o => o.Shipment)
            .Include(o => o.ProductInfo).ThenInclude(pi => pi!.Product)
            .Include(o => o.Status);
    }
}

