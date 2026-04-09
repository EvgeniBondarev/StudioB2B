using StudioB2B.Domain.Constants;

namespace StudioB2B.Shared;

public class CommunicationTaskDto
{
    public Guid Id { get; set; }

    public CommunicationTaskType TaskType { get; set; }

    public string ExternalId { get; set; } = string.Empty;

    public Guid MarketplaceClientId { get; set; }

    public string MarketplaceClientName { get; set; } = string.Empty;

    public CommunicationTaskStatus Status { get; set; }

    public Guid? AssignedToUserId { get; set; }

    public string? AssignedToUserName { get; set; }

    public DateTime? AssignedAt { get; set; }

    public DateTime? StartedAt { get; set; }

    public DateTime? CompletedAt { get; set; }

    public long TotalTimeSpentTicks { get; set; }

    public string Title { get; set; } = string.Empty;

    public string? PreviewText { get; set; }

    public string? ExternalStatus { get; set; }

    /// <summary>Ozon chat type (BUYER_SELLER / BUYER_SUPPORT / etc.). Null for non-chat tasks.</summary>
    public string? ChatType { get; set; }

    /// <summary>Unread message count (chat tasks only).</summary>
    public int UnreadCount { get; set; }

    public string? ExternalUrl { get; set; }

    public decimal? PaymentAmount { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime UpdatedAt { get; set; }

    /// <summary>True if a time entry with EndedAt == null exists for this task.</summary>
    public bool HasActiveTimer { get; set; }

    /// <summary>Чат: последнее сообщение в переписке от покупателя (нужен ответ продавца). Для остальных типов — false.</summary>
    public bool LastMessageFromCustomer { get; set; }

    /// <summary>Задача была авто-переоткрыта после завершения из-за нового сообщения покупателя.</summary>
    public bool WasPreviouslyCompleted { get; set; }
}

