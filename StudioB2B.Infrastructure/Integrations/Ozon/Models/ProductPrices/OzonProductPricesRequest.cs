using System.Text.Json.Serialization;

namespace StudioB2B.Infrastructure.Integrations.Ozon.Models.ProductPrices;

public class OzonProductPricesRequest
{
    [JsonPropertyName("cursor")]
    public string Cursor { get; set; } = string.Empty;

    [JsonPropertyName("filter")]
    public OzonProductPricesFilter Filter { get; set; } = new();

    [JsonPropertyName("limit")]
    public int Limit { get; set; } = 100;
}

public class OzonProductPricesFilter
{
    [JsonPropertyName("offer_id")]
    public List<string> OfferId { get; set; } = new();

    [JsonPropertyName("visibility")]
    public string Visibility { get; set; } = "ALL";
}
