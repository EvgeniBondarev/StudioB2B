using System.Text.Json.Serialization;

namespace StudioB2B.Shared;

public class OzonQuestionAnswerDeleteResponseDto
{
    [JsonPropertyName("code")]
    public int Code { get; set; }

    [JsonPropertyName("details")]
    public string? Details { get; set; }

    [JsonPropertyName("message")]
    public string? Message { get; set; }
}
