using System.Text.Json.Serialization;

namespace StudioB2B.Infrastructure.Integrations.Ozon.Models.FbsUnfulfilled;

public class OzonFbsUnfulfilledListResponse
{
    [JsonPropertyName("result")]
    public OzonFbsUnfulfilledResult? Result { get; set; }
}

public class OzonFbsUnfulfilledResult
{
    [JsonPropertyName("count")]
    public int Count { get; set; }

    [JsonPropertyName("postings")]
    public List<OzonFbsPostingDto> Postings { get; set; } = new();
}
