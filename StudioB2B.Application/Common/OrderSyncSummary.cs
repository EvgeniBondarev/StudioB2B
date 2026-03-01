namespace StudioB2B.Application.Common;

/// <summary>
/// Итог синхронизации: сводка по всем клиентам и разбивка по каждому клиенту.
/// </summary>
public class OrderSyncSummary
{
    public OrderSyncResult Total { get; set; } = new();
    public List<ClientSyncResultItem> PerClient { get; set; } = new();
    /// <summary>Детализация по каждому обновлённому отправлению (для режима обновления).</summary>
    public List<ShipmentUpdateItem> UpdatedShipments { get; set; } = new();
}
