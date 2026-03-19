using System.Text.Json.Serialization;

namespace StudioB2B.Domain.Constants;

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum OzonQuestionStatusEnum
{
    /// <summary>Новый вопрос.</summary>
    NEW,

    /// <summary>Все вопросы (значение фильтра, в ответе Ozon возвращает конкретный статус).</summary>
    ALL,

    /// <summary>Просмотренный.</summary>
    VIEWED,

    /// <summary>Обработанный.</summary>
    PROCESSED,

    /// <summary>Необработанный.</summary>
    UNPROCESSED
}

