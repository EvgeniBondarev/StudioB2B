using System.Text.Json.Serialization;

namespace StudioB2B.Shared;

public class OzonReturnMoneyDto
{
    [JsonPropertyName("currency_code")]
    public string? CurrencyCode { get; set; }

    [JsonPropertyName("price")]
    public decimal? Price { get; set; }
}
