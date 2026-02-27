using System.Text.Json.Serialization;

namespace StudioB2B.Infrastructure.Integrations.Ozon.Models.FbsUnfulfilled;

public class OzonFbsDeliveryMethodDto
{
    [JsonPropertyName("id")]
    public long Id { get; set; }

    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    [JsonPropertyName("warehouse_id")]
    public long WarehouseId { get; set; }

    [JsonPropertyName("warehouse")]
    public string? Warehouse { get; set; }

    [JsonPropertyName("tpl_provider_id")]
    public long? TplProviderId { get; set; }

    [JsonPropertyName("tpl_provider")]
    public string? TplProvider { get; set; }
}
