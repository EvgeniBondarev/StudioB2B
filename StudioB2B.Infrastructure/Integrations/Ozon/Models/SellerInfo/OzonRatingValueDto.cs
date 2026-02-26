using System.Text.Json.Serialization;

namespace StudioB2B.Infrastructure.Integrations.Ozon.Models.SellerInfo;

public class OzonRatingValueDto
{
    [JsonPropertyName("date_from")]
    public DateTimeOffset? DateFrom { get; set; }

    [JsonPropertyName("date_to")]
    public DateTimeOffset? DateTo { get; set; }

    [JsonPropertyName("formatted")]
    public string? Formatted { get; set; }

    [JsonPropertyName("status")]
    public OzonRatingValueStatusDto? Status { get; set; }

    [JsonPropertyName("value")]
    public decimal Value { get; set; }
}

