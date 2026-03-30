using System.Text.Json.Serialization;

namespace StudioB2B.Shared;

public class OzonQuestionListRequestDto
{
    [JsonPropertyName("filter")]
    public OzonQuestionListFilterDto? Filter { get; set; }

    /// <summary>
    /// Идентификатор последнего значения на странице. Пустая строка для первого запроса.
    /// </summary>
    [JsonPropertyName("last_id")]
    public string? LastId { get; set; }
}
