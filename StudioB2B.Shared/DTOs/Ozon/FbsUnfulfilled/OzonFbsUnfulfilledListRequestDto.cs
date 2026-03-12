using System.Text.Json.Serialization;

namespace StudioB2B.Shared.DTOs;

public class OzonFbsUnfulfilledListRequestDto
{
    [JsonPropertyName("filter")]
    public OzonFbsUnfulfilledFilterDto Filter { get; set; } = new();

    [JsonPropertyName("limit")]
    public int Limit { get; set; } = 100;

    [JsonPropertyName("offset")]
    public int Offset { get; set; } = 0;

    [JsonPropertyName("with")]
    public OzonFbsUnfulfilledWithDto With { get; set; } = new();

    [JsonPropertyName("dir")]
    public string Dir { get; set; } = "asc";
}
