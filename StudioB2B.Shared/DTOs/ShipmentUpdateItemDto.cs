namespace StudioB2B.Shared.DTOs;

/// <summary>
/// Информация об обновлении одного отправления: старый и новый статус.
/// </summary>
public class ShipmentUpdateItemDto
{
    public string PostingNumber { get; set; } = string.Empty;

    public string ClientName { get; set; } = string.Empty;

    public string OldStatusName { get; set; } = string.Empty;

    public string NewStatusName { get; set; } = string.Empty;
}

