using System.Text.Json.Serialization;

namespace StudioB2B.Shared;

public class OzonProductPricesFilterDto
{
    [JsonPropertyName("offer_id")]
    public List<string> OfferId { get; set; } = new();

    [JsonPropertyName("visibility")]
    public string Visibility { get; set; } = "ALL";
}
