namespace StudioB2B.Application.Common;

/// <summary>
/// Результат синхронизации заказов: количество созданных, обновлённых и не тронутых отправлений и заказов.
/// </summary>
public class OrderSyncResult
{
    public int ShipmentsCreated { get; set; }
    public int ShipmentsUpdated { get; set; }
    /// <summary>Отправления в выборке, по которым не было изменений (пока не используется, всегда 0).</summary>
    public int ShipmentsUntouched { get; set; }
    public int OrdersCreated { get; set; }
    public int OrdersUpdated { get; set; }
    /// <summary>Заказы в выборке, по которым не было изменений (пока не используется, всегда 0).</summary>
    public int OrdersUntouched { get; set; }

    public void Add(OrderSyncResult other)
    {
        ShipmentsCreated += other.ShipmentsCreated;
        ShipmentsUpdated += other.ShipmentsUpdated;
        ShipmentsUntouched += other.ShipmentsUntouched;
        OrdersCreated += other.OrdersCreated;
        OrdersUpdated += other.OrdersUpdated;
        OrdersUntouched += other.OrdersUntouched;
    }
}
