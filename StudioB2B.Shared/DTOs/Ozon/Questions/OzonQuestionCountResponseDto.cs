using System.Text.Json.Serialization;

namespace StudioB2B.Shared.DTOs;

public class OzonQuestionCountResponseDto
{
    [JsonPropertyName("all")]
    public long All { get; set; }

    [JsonPropertyName("new")]
    public long New { get; set; }

    [JsonPropertyName("processed")]
    public long Processed { get; set; }

    [JsonPropertyName("unprocessed")]
    public long Unprocessed { get; set; }

    [JsonPropertyName("viewed")]
    public long Viewed { get; set; }
}
