using System.Text.Json.Serialization;

namespace StudioB2B.Shared;

public class OzonReviewPhotoDto
{
    [JsonPropertyName("height")]
    public int Height { get; set; }

    [JsonPropertyName("url")]
    public string Url { get; set; } = string.Empty;

    [JsonPropertyName("width")]
    public int Width { get; set; }
}
