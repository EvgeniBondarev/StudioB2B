namespace StudioB2B.Shared;

public class OzonPushNotificationDto
{
    public Guid Id { get; set; }

    public string MessageType { get; set; } = string.Empty;

    public string RawPayload { get; set; } = string.Empty;

    public long? SellerId { get; set; }

    public string? PostingNumber { get; set; }

    /// <summary>chat_id для chat-типов уведомлений (TYPE_NEW_MESSAGE, TYPE_UPDATE_MESSAGE, TYPE_MESSAGE_READ, TYPE_CHAT_CLOSED).</summary>
    public string? ChatId { get; set; }

    /// <summary>Текст первого сообщения (data[0]) для TYPE_NEW_MESSAGE и TYPE_UPDATE_MESSAGE.</summary>
    public string? MessageText { get; set; }

    public DateTime ReceivedAtUtc { get; set; }

    public Guid? MarketplaceClientId { get; set; }

    public string? MarketplaceClientName { get; set; }
}

