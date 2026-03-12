using System.Text.Json.Serialization;

namespace StudioB2B.Domain.Constants;

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum OzonSubscriptionTypeEnum
{
    UNKNOWN,
    UNSPECIFIED,
    PREMIUM,
    PREMIUM_LITE,
    PREMIUM_PLUS,
    PREMIUM_PRO
}
