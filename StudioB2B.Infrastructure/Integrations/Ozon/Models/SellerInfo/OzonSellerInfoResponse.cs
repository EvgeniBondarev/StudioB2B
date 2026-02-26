using System.Text.Json.Serialization;

namespace StudioB2B.Infrastructure.Integrations.Ozon.Models.SellerInfo;

public class OzonSellerInfoResponse
{
    [JsonPropertyName("company")]
    public OzonCompanyDto? Company { get; set; }

    [JsonPropertyName("ratings")]
    public List<OzonRatingDto> Ratings { get; set; } = new();

    [JsonPropertyName("subscription")]
    public OzonSubscriptionDto? Subscription { get; set; }
}

