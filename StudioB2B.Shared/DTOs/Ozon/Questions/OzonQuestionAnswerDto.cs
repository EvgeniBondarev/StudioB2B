using System.Text.Json.Serialization;

namespace StudioB2B.Shared;

public class OzonQuestionAnswerDto
{
    [JsonPropertyName("id")]
    public string Id { get; set; } = string.Empty;

    [JsonPropertyName("question_id")]
    public string QuestionId { get; set; } = string.Empty;

    [JsonPropertyName("sku")]
    public long Sku { get; set; }

    [JsonPropertyName("author_name")]
    public string AuthorName { get; set; } = string.Empty;

    [JsonPropertyName("text")]
    public string Text { get; set; } = string.Empty;

    [JsonPropertyName("published_at")]
    public DateTime? PublishedAt { get; set; }
}
