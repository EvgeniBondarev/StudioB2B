using System.Text.Json.Serialization;

namespace StudioB2B.Domain.Constants;

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum OzonRatingStatusEnum
{
    UNKNOWN,
    OK,
    WARNING,
    CRITICAL
}
