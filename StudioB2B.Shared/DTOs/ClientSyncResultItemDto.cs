namespace StudioB2B.Shared.DTOs;

/// <summary>
/// Результат синхронизации по одному клиенту маркетплейса.
/// </summary>
public class ClientSyncResultItemDto
{
    public string ClientName { get; set; } = string.Empty;

    public string Mode { get; set; } = string.Empty;

    public int ShipmentsCreated { get; set; }

    public int ShipmentsUpdated { get; set; }

    public int ShipmentsUntouched { get; set; }

    public int OrdersCreated { get; set; }

    public int OrdersUpdated { get; set; }

    public int OrdersUntouched { get; set; }

    public int OrdersSelectedForUpdate { get; set; }

    public int OrdersSkipped { get; set; }

    public string? UpdatedFieldsSummary { get; set; }
}

