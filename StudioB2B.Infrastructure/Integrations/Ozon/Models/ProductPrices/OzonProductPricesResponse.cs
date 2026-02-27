using System.Text.Json.Serialization;

namespace StudioB2B.Infrastructure.Integrations.Ozon.Models.ProductPrices;

public class OzonProductPricesResponse
{
    [JsonPropertyName("cursor")]
    public string? Cursor { get; set; }

    [JsonPropertyName("items")]
    public List<OzonProductPriceItemDto> Items { get; set; } = new();

    [JsonPropertyName("total")]
    public int Total { get; set; }
}
