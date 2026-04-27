namespace StudioB2B.Shared;

public class OpenRouterChatRequestDto
{
    public string Message { get; set; } = "";

    public string? SystemPrompt { get; set; }

    public string? Model { get; set; }

    public decimal? Temperature { get; set; }

    public int? MaxTokens { get; set; }
}
