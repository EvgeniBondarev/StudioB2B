using System.Text.Json.Serialization;

namespace StudioB2B.Shared;

public class OzonRatingValueStatusDto
{
    [JsonPropertyName("danger")]
    public bool Danger { get; set; }

    [JsonPropertyName("premium")]
    public bool Premium { get; set; }

    [JsonPropertyName("warning")]
    public bool Warning { get; set; }
}

