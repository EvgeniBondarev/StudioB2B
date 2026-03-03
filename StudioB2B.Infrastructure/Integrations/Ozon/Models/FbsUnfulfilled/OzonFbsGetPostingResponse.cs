using System.Text.Json.Serialization;

namespace StudioB2B.Infrastructure.Integrations.Ozon.Models.FbsUnfulfilled;

public class OzonFbsGetPostingResponse
{
    [JsonPropertyName("result")]
    public OzonFbsPostingDto? Result { get; set; }
}
