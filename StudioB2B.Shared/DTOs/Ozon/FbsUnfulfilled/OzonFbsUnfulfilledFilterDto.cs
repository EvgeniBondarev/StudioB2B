using System.Text.Json.Serialization;

namespace StudioB2B.Shared;

public class OzonFbsUnfulfilledFilterDto
{
    [JsonPropertyName("cutoff_from")]
    public DateTime CutoffFrom { get; set; }

    [JsonPropertyName("cutoff_to")]
    public DateTime CutoffTo { get; set; }
}
