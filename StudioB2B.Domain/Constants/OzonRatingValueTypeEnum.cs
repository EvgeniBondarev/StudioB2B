using System.Text.Json.Serialization;

namespace StudioB2B.Domain.Constants;

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum OzonRatingValueTypeEnum
{
    UNKNOWN,
    INDEX,
    PERCENT,
    TIME,
    RATIO,
    REVIEW_SCORE,
    COUNT
}
