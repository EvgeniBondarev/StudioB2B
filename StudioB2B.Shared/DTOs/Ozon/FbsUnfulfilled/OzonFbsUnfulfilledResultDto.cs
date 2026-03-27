using System.Text.Json.Serialization;

namespace StudioB2B.Shared;

public class OzonFbsUnfulfilledResultDto
{
    [JsonPropertyName("count")]
    public int Count { get; set; }

    [JsonPropertyName("postings")]
    public List<OzonFbsPostingDto> Postings { get; set; } = new();
}
