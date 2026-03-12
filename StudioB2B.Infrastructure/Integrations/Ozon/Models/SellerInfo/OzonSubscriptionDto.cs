using System.Text.Json.Serialization;
using StudioB2B.Domain.Constants;

namespace StudioB2B.Infrastructure.Integrations.Ozon.Models.SellerInfo;

public class OzonSubscriptionDto
{
    [JsonPropertyName("is_premium")]
    public bool IsPremium { get; set; }

    [JsonPropertyName("type")]
    public OzonSubscriptionTypeEnum Type { get; set; }
}

