using System.Text.Json.Serialization;

namespace StudioB2B.Infrastructure.Integrations.Ozon.Models.ProductPrices;

/// <summary>
/// Итоговые индексы цены товара и данные по внешним ценам.
/// </summary>
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

/// <summary>
/// Данные об индексах цены: минимальная цена и само значение индекса.
/// </summary>
public class OzonProductPriceIndexDataDto
{
    [JsonPropertyName("min_price")]
    public decimal? MinPrice { get; set; }

    [JsonPropertyName("min_price_currency")]
    public string? MinPriceCurrency { get; set; }

    [JsonPropertyName("price_index_value")]
    public decimal? PriceIndexValue { get; set; }
}

