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

