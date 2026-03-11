using System.Text.Json.Serialization;

namespace StudioB2B.Domain.Constants;

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum OzonTaxSystemEnum
{
    UNKNOWN,
    UNSPECIFIED,
    OSNO,
    USN,
    NPD,
    AUSN,
    PSN
}
