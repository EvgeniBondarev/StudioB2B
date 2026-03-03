namespace StudioB2B.Application.Common;

/// <summary>
/// Результат синхронизации по одному клиенту маркетплейса.
/// </summary>
public class ClientSyncResultItem
{
    public string ClientName { get; set; } = string.Empty;
    public string Mode { get; set; } = string.Empty;
    public int ShipmentsCreated { get; set; }
    public int ShipmentsUpdated { get; set; }
    public int ShipmentsUntouched { get; set; }
    public int OrdersCreated { get; set; }
    public int OrdersUpdated { get; set; }
    public int OrdersUntouched { get; set; }
    /// <summary>Сводка обновлённых полей по отправлениям (для режима обновления).</summary>
    public string? UpdatedFieldsSummary { get; set; }
}
