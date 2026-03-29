using System.Text.Json.Serialization;
using StudioB2B.Domain.Constants;

namespace StudioB2B.Shared;

public class OzonQuestionListFilterDto
{
    [JsonPropertyName("date_from")]
    public DateTime? DateFrom { get; set; }

    [JsonPropertyName("date_to")]
    public DateTime? DateTo { get; set; }

    /// <summary>
    /// Статус вопроса. Значение ALL запрашивает все, но в ответе Ozon возвращает конкретные статусы.
    /// </summary>
    [JsonPropertyName("status")]
    public OzonQuestionStatusEnum? Status { get; set; }
}

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

