using System.Text.Json.Serialization;

namespace StudioB2B.Shared.DTOs;

public class OzonReviewCountResponseDto
{
    [JsonPropertyName("processed")]
    public int Processed { get; set; }

    [JsonPropertyName("total")]
    public int Total { get; set; }

    [JsonPropertyName("unprocessed")]
    public int Unprocessed { get; set; }
}

