namespace StudioB2B.Shared.DTOs;

public class OzonQuestionViewModelDto
{
    public Guid MarketplaceClientId { get; set; }
    public string MarketplaceClientName { get; set; } = string.Empty;

    public string ApiId { get; set; } = string.Empty;
    public string ApiKey { get; set; } = string.Empty;

    public string Id { get; set; } = string.Empty;
    public long Sku { get; set; }
    public string ProductUrl { get; set; } = string.Empty;
    public string QuestionLink { get; set; } = string.Empty;

    public string AuthorName { get; set; } = string.Empty;
    public string Text { get; set; } = string.Empty;

    public string Status { get; set; } = string.Empty;
    public int AnswersCount { get; set; }

    public DateTime PublishedAt { get; set; }
}

