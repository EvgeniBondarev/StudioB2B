using System.Text.Json.Serialization;

namespace StudioB2B.Infrastructure.Integrations.Ozon.Models;

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum OzonTaxSystem
{
    UNKNOWN,
    UNSPECIFIED,
    OSNO,
    USN,
    NPD,
    AUSN,
    PSN
}

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum OzonRatingStatus
{
    UNKNOWN,
    OK,
    WARNING,
    CRITICAL
}

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum OzonRatingValueType
{
    UNKNOWN,
    INDEX,
    PERCENT,
    TIME,
    RATIO,
    REVIEW_SCORE,
    COUNT
}

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum OzonSubscriptionType
{
    UNKNOWN,
    UNSPECIFIED,
    PREMIUM,
    PREMIUM_LITE,
    PREMIUM_PLUS,
    PREMIUM_PRO
}
