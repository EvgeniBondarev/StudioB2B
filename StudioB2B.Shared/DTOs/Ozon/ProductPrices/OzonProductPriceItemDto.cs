using System.Text.Json.Serialization;

namespace StudioB2B.Shared;

/// <summary>
/// Один товар из ответа /v5/product/info/prices.
/// </summary>
public class OzonProductPriceItemDto
{
    [JsonPropertyName("offer_id")]
    public string OfferId { get; set; } = string.Empty;

    [JsonPropertyName("product_id")]
    public long ProductId { get; set; }

    [JsonPropertyName("price")]
    public OzonProductPriceDto? Price { get; set; }

    [JsonPropertyName("commissions")]
    public OzonProductCommissionsDto? Commissions { get; set; }

    [JsonPropertyName("price_indexes")]
    public OzonProductPriceIndexesDto? PriceIndexes { get; set; }
}

