using System.Text.Json.Serialization;

namespace StudioB2B.Infrastructure.Integrations.Ozon.Models.ProductAttributes;

public class OzonProductAttributesRequest
{
    [JsonPropertyName("filter")]
    public OzonProductAttributesFilter Filter { get; set; } = new();

    [JsonPropertyName("last_id")]
    public string LastId { get; set; } = string.Empty;

    [JsonPropertyName("limit")]
    public int Limit { get; set; } = 1000;

    [JsonPropertyName("sort_dir")]
    public string SortDir { get; set; } = "ASC";
}

public class OzonProductAttributesFilter
{
    [JsonPropertyName("offer_id")]
    public List<string> OfferId { get; set; } = new();

    [JsonPropertyName("visibility")]
    public string Visibility { get; set; } = "ALL";
}
