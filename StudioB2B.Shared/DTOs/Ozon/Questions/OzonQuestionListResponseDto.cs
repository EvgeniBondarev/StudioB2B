using System.Text.Json.Serialization;

namespace StudioB2B.Shared;

public class OzonQuestionListResponseDto
{
    [JsonPropertyName("questions")] public List<OzonQuestionItemDto> Questions { get; set; } = new();

    /// <summary>Идентификатор последнего значения на странице. Передаётся в следующий запрос как last_id.</summary>
    [JsonPropertyName("last_id")] public string? LastId { get; set; }
}
