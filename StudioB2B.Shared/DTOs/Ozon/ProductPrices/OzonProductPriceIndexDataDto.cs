using System.Text.Json.Serialization;

namespace StudioB2B.Shared.DTOs;

public class OzonProductPriceIndexDataDto
{
    [JsonPropertyName("min_price")]
    public decimal? MinPrice { get; set; }

    [JsonPropertyName("min_price_currency")]
    public string? MinPriceCurrency { get; set; }

    [JsonPropertyName("price_index_value")]
    public decimal? PriceIndexValue { get; set; }
}
