namespace StudioB2B.Shared;

/// <summary>
/// Итог синхронизации: сводка по всем клиентам и разбивка по каждому клиенту.
/// </summary>
public class OrderSyncSummaryDto
{
    public OrderSyncResultDto Total { get; set; } = new();

    public List<ClientSyncResultItemDto> PerClient { get; set; } = new();

    /// <summary>Детализация по каждому обновлённому отправлению (для режима обновления).</summary>
    public List<ShipmentUpdateItemDto> UpdatedShipments { get; set; } = new();
}

