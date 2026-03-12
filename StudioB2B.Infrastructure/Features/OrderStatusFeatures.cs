using Microsoft.EntityFrameworkCore;
using StudioB2B.Domain.Entities;
using StudioB2B.Infrastructure.Persistence.Tenant;

namespace StudioB2B.Infrastructure.Features;

public static class OrderStatusExtensions
{
    /// <summary>
    /// Базовый набор Include для статусов заказов.
    /// </summary>
    public static IQueryable<OrderStatus> IncludeEverything(this IQueryable<OrderStatus> q)
    {
        return q
            .Include(s => s.MarketplaceClientType);
    }

    /// <summary>
    /// Только не удалённые статусы.
    /// </summary>
    public static IQueryable<OrderStatus> Active(this IQueryable<OrderStatus> q)
    {
        return q.Where(s => !s.IsDeleted);
    }

    /// <summary>
    /// Создание нового статуса.
    /// </summary>
    public static async Task<OrderStatus> CreateOrderStatusAsync(this TenantDbContext db, OrderStatus status,
                                                                 CancellationToken ct = default)
    {
        db.OrderStatuses.Add(status);
        await db.SaveChangesAsync(ct);
        return status;
    }

    /// <summary>
    /// Обновление существующего статуса.
    /// </summary>
    public static async Task UpdateOrderStatusAsync(this TenantDbContext db, OrderStatus status,
                                                    CancellationToken ct = default)
    {
        if (!db.OrderStatuses.Local.Contains(status))
        {
            db.OrderStatuses.Attach(status).State = EntityState.Modified;
        }

        await db.SaveChangesAsync(ct);
    }

    /// <summary>
    /// Мягкое удаление статуса (IsDeleted = true).
    /// </summary>
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

