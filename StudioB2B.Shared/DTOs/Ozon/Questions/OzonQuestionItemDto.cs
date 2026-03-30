using System.Text.Json.Serialization;
using StudioB2B.Domain.Constants;

namespace StudioB2B.Shared;

public class OzonQuestionItemDto
{
    [JsonPropertyName("answers_count")]
    public long AnswersCount { get; set; }

    [JsonPropertyName("author_name")]
    public string AuthorName { get; set; } = string.Empty;

    [JsonPropertyName("id")]
    public string Id { get; set; } = string.Empty;

    [JsonPropertyName("product_url")]
    public string ProductUrl { get; set; } = string.Empty;

    [JsonPropertyName("published_at")]
    public DateTime PublishedAt { get; set; }

    [JsonPropertyName("question_link")]
    public string QuestionLink { get; set; } = string.Empty;

    [JsonPropertyName("sku")]
    public long Sku { get; set; }

    [JsonPropertyName("status")]
    public OzonQuestionStatusEnum Status { get; set; }

    [JsonPropertyName("text")]
    public string Text { get; set; } = string.Empty;
}
