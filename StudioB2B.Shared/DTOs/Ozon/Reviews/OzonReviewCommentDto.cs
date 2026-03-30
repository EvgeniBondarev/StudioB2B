using System.Text.Json.Serialization;

namespace StudioB2B.Shared;

public class OzonReviewCommentDto
{
    [JsonPropertyName("id")]
    public string Id { get; set; } = string.Empty;

    [JsonPropertyName("is_official")]
    public bool IsOfficial { get; set; }

    [JsonPropertyName("is_owner")]
    public bool IsOwner { get; set; }

    [JsonPropertyName("parent_comment_id")]
    public string ParentCommentId { get; set; } = string.Empty;

    [JsonPropertyName("published_at")]
    public DateTime PublishedAt { get; set; }

    [JsonPropertyName("text")]
    public string Text { get; set; } = string.Empty;
}
