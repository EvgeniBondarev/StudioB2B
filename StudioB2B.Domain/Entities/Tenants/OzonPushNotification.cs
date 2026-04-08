namespace StudioB2B.Domain.Entities;

/// <summary>
/// Входящее push-уведомление от Ozon, сохранённое в базу данных тенанта.
/// </summary>
public class OzonPushNotification : IBaseEntity
{
    public Guid Id { get; set; }

    /// <summary>Тип уведомления (TYPE_NEW_POSTING, TYPE_PING, …).</summary>
    public string MessageType { get; set; } = string.Empty;

    /// <summary>Полный JSON-текст уведомления как получен от Ozon.</summary>
    public string RawPayload { get; set; } = string.Empty;

    /// <summary>seller_id из payload — идентификатор продавца Ozon.</summary>
    public long? SellerId { get; set; }

    /// <summary>posting_number из payload (для уведомлений об отправлениях).</summary>
    public string? PostingNumber { get; set; }

    /// <summary>Время получения уведомления нашим сервером (UTC).</summary>
    public DateTime ReceivedAtUtc { get; set; } = DateTime.UtcNow;

    /// <summary>FK на MarketplaceClient, определённый по seller_id.</summary>
    public Guid? MarketplaceClientId { get; set; }

    public MarketplaceClient? MarketplaceClient { get; set; }
}

