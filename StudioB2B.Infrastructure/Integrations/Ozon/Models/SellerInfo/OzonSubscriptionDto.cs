using System.Text.Json.Serialization;

namespace StudioB2B.Infrastructure.Integrations.Ozon.Models.SellerInfo;

public class OzonSubscriptionDto
{
    [JsonPropertyName("is_premium")]
    public bool IsPremium { get; set; }

    [JsonPropertyName("type")]
    public OzonSubscriptionType Type { get; set; }
}

