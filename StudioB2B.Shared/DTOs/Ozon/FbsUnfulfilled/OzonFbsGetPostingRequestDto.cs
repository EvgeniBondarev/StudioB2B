using System.Text.Json.Serialization;

namespace StudioB2B.Shared.DTOs;

public class OzonFbsGetPostingRequestDto
{
    [JsonPropertyName("posting_number")]
    public string PostingNumber { get; set; } = string.Empty;

    [JsonPropertyName("with")]
    public OzonFbsGetPostingWithDto With { get; set; } = new();
}
