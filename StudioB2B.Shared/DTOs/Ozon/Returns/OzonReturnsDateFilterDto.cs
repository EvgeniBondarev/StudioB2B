using System.Text.Json.Serialization;

namespace StudioB2B.Shared.DTOs;

public class OzonReturnsDateFilterDto
{
    [JsonPropertyName("time_from")]
    public DateTime? TimeFrom { get; set; }

    [JsonPropertyName("time_to")]
    public DateTime? TimeTo { get; set; }
}
