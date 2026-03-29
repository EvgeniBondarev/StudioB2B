using System.Text.Json.Serialization;

namespace StudioB2B.Shared;

public class OzonFboPostingListFilterDto
{
    [JsonPropertyName("since")]
    public DateTime Since { get; set; }

    [JsonPropertyName("status")]
    public string Status { get; set; } = string.Empty;

    [JsonPropertyName("to")]
    public DateTime To { get; set; }
}

