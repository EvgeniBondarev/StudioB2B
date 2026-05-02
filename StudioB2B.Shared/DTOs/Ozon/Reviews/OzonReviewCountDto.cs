using System.Text.Json.Serialization;

namespace StudioB2B.Shared;

public class OzonReviewCountResponseDto
{
    [JsonPropertyName("new")]
    public int New { get; set; }

    [JsonPropertyName("processed")]
    public int Processed { get; set; }

    [JsonPropertyName("total")]
    public int Total { get; set; }

    [JsonPropertyName("viewed")]
    public int Viewed { get; set; }
}

