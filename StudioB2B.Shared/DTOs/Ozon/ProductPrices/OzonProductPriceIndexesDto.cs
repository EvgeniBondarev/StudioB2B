using System.Text.Json.Serialization;

namespace StudioB2B.Shared.DTOs;

public class OzonProductPriceIndexesDto
{
    [JsonPropertyName("color_index")]
    public string? ColorIndex { get; set; }

    [JsonPropertyName("external_index_data")]
    public OzonProductPriceIndexDataDto? ExternalIndexData { get; set; }

    [JsonPropertyName("ozon_index_data")]
    public OzonProductPriceIndexDataDto? OzonIndexData { get; set; }

    [JsonPropertyName("self_marketplaces_index_data")]
    public OzonProductPriceIndexDataDto? SelfMarketplacesIndexData { get; set; }
}
