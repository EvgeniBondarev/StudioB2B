namespace StudioB2B.Shared;

public class OzonChatViewModelDto
{
    public Guid MarketplaceClientId { get; set; }

    public string MarketplaceClientName { get; set; } = string.Empty;

    public string ApiId { get; set; } = string.Empty;

    public string ApiKey { get; set; } = string.Empty;

    public string ChatId { get; set; } = string.Empty;

    public string ChatStatus { get; set; } = string.Empty;

    public string ChatType { get; set; } = string.Empty;

    public DateTime CreatedAt { get; set; }

    public ulong? FirstUnreadMessageId { get; set; }

    public ulong? LastMessageId { get; set; }

    public int UnreadCount { get; set; }

    public DateTime LastMessageAt { get; set; }
}
