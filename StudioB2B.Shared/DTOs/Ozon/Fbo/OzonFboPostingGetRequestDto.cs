using System.Text.Json.Serialization;

namespace StudioB2B.Shared.DTOs;

public class OzonFboPostingGetRequestDto
{
    [JsonPropertyName("posting_number")]
    public string PostingNumber { get; set; } = string.Empty;

    [JsonPropertyName("translit")]
    public bool TranslIt { get; set; } = true;

    [JsonPropertyName("with")]
    public OzonFboPostingGetWithDto With { get; set; } = new();
}

