using System.Text.Json.Serialization;

namespace StudioB2B.Infrastructure.Integrations.Ozon.Models.ProductAttributes;

public class OzonProductAttributesResponse
{
    [JsonPropertyName("result")]
    public List<OzonProductAttributeItemDto> Result { get; set; } = new();

    [JsonPropertyName("last_id")]
    public string? LastId { get; set; }

    [JsonPropertyName("total")]
    public long Total { get; set; }
}
