using System.Text.Json.Serialization;

namespace StudioB2B.Shared.DTOs;

public class OzonReviewVideoDto
{
    [JsonPropertyName("height")]                    public int    Height                  { get; set; }
    [JsonPropertyName("preview_url")]               public string PreviewUrl              { get; set; } = string.Empty;
    [JsonPropertyName("short_video_preview_url")]   public string ShortVideoPreviewUrl    { get; set; } = string.Empty;
    [JsonPropertyName("url")]                       public string Url                     { get; set; } = string.Empty;
    [JsonPropertyName("width")]                     public int    Width                   { get; set; }
}
