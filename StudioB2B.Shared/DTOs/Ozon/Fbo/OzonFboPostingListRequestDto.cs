using System.Text.Json.Serialization;

namespace StudioB2B.Shared;

public class OzonFboPostingListRequestDto
{
    [JsonPropertyName("dir")]
    public string Dir { get; set; } = "ASC";

    [JsonPropertyName("filter")]
    public OzonFboPostingListFilterDto Filter { get; set; } = new();

    [JsonPropertyName("limit")]
    public int Limit { get; set; } = 100;

    [JsonPropertyName("offset")]
    public int Offset { get; set; } = 0;

    [JsonPropertyName("translit")]
    public bool TranslIt { get; set; } = true;

    [JsonPropertyName("with")]
    public OzonFboPostingListWithDto With { get; set; } = new();
}

