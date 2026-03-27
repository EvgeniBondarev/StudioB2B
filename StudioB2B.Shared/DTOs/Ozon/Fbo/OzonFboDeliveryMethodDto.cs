using System.Text.Json.Serialization;

namespace StudioB2B.Shared;

public class OzonFboDeliveryMethodDto
{
    // Note: в FBO ответе эти поля находятся в `analytics_data`.
    [JsonPropertyName("delivery_type")]
    public string Name { get; set; } = string.Empty;

    [JsonPropertyName("warehouse_id")]
    public long Id { get; set; }

    [JsonPropertyName("warehouse_name")]
    public string? Warehouse { get; set; }

    public long WarehouseId => Id;
}

