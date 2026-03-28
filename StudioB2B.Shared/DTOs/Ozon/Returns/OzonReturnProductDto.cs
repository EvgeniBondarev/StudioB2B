using System.Text.Json.Serialization;

namespace StudioB2B.Shared;

public class OzonReturnProductDto
{
    [JsonPropertyName("sku")]
    public long? Sku { get; set; }

    [JsonPropertyName("offer_id")]
    public string? OfferId { get; set; }

    [JsonPropertyName("name")]
    public string? Name { get; set; }

    [JsonPropertyName("price")]
    public OzonReturnMoneyDto? Price { get; set; }

    [JsonPropertyName("price_without_commission")]
    public OzonReturnMoneyDto? PriceWithoutCommission { get; set; }

    [JsonPropertyName("commission_percent")]
    public decimal? CommissionPercent { get; set; }

    [JsonPropertyName("commission")]
    public OzonReturnMoneyDto? Commission { get; set; }

    [JsonPropertyName("quantity")]
    public int Quantity { get; set; }
}
