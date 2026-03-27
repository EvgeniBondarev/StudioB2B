using System.Text.Json.Serialization;

namespace StudioB2B.Shared;

public class OzonProductAttributesFilterDto
{
    [JsonPropertyName("offer_id")]
    public List<string> OfferId { get; set; } = new();

    [JsonPropertyName("product_id")]
    public List<string> ProductId { get; set; } = new();

    [JsonPropertyName("sku")]
    public List<string> Sku { get; set; } = new();

    [JsonPropertyName("visibility")]
    public string Visibility { get; set; } = "ALL";
}
