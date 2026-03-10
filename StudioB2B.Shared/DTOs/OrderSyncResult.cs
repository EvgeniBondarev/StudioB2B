namespace StudioB2B.Shared.DTOs;

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

    /// <summary>Заказы, выбранные для обновления статуса (без конечного статуса). Для проверки фильтрации.</summary>
    public int OrdersSelectedForUpdate { get; set; }

    /// <summary>Заказы, пропущенные из-за конечного статуса.</summary>
    public int OrdersSkipped { get; set; }

    /// <summary>Сводка обновлённых полей (для режима обновления статусов), например: «Статус, Номер заказа, Дата отгрузки».</summary>
    public string? UpdatedFieldsSummary { get; set; }

    /// <summary>Детализация по каждому обновлённому отправлению (старый и новый статус).</summary>
    public List<ShipmentUpdateItem> UpdatedShipments { get; set; } = new();

    public void Add(OrderSyncResult other)
    {
        ShipmentsCreated += other.ShipmentsCreated;
        ShipmentsUpdated += other.ShipmentsUpdated;
        ShipmentsUntouched += other.ShipmentsUntouched;
        OrdersCreated += other.OrdersCreated;
        OrdersUpdated += other.OrdersUpdated;
        OrdersUntouched += other.OrdersUntouched;
        OrdersSelectedForUpdate += other.OrdersSelectedForUpdate;
        OrdersSkipped += other.OrdersSkipped;
    }
}
