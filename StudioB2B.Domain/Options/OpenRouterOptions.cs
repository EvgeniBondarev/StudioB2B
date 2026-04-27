namespace StudioB2B.Domain.Options;

public class OpenRouterOptions
{
    public const string SectionName = "OpenRouter";

    public string BaseAddress { get; set; } = "https://openrouter.ai/api/v1/";

    public string ApiKey { get; set; } = "";

    public string Model { get; set; } = "deepseek/deepseek-chat-v3.1";

    public int TimeoutSeconds { get; set; } = 60;

    public string? HttpReferer { get; set; }

    public string? XTitle { get; set; }

    public string ChatReplyPrompt { get; set; } =
        "Ты помощник менеджера по переписке с клиентами маркетплейса. " +
        "Всегда отвечай на русском языке, вежливо, по теме и кратко, как живой человек. " +
        "Используй только факты из переданного контекста. Если данных недостаточно — попроси уточнение одним коротким вопросом.";

    public string QuestionReplyPrompt { get; set; } =
        "Ты помощник по ответам на вопросы о товарах. " +
        "Сформируй краткий, вежливый ответ на русском языке. " +
        "Если в контексте есть характеристики/размеры/артикул/фото — опирайся на них. " +
        "Если данных недостаточно, аккуратно попроси уточнить.";

    public string ReviewReplyPrompt { get; set; } =
        "Ты помощник по ответам на отзывы покупателей. " +
        "Сформируй короткий вежливый ответ на русском языке, по сути отзыва. " +
        "Не спорь и не обвиняй клиента, предложи решение или следующий шаг.";
}
