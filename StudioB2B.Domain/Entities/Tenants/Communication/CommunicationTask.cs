using StudioB2B.Domain.Constants;

namespace StudioB2B.Domain.Entities;

public class CommunicationTask : IBaseEntity, ISoftDelete
{
    public Guid Id { get; set; }

    public CommunicationTaskType TaskType { get; set; }

    /// <summary>ChatId, QuestionId, or ReviewId from Ozon.</summary>
    public string ExternalId { get; set; } = string.Empty;

    public Guid MarketplaceClientId { get; set; }
    public MarketplaceClient? MarketplaceClient { get; set; }

    public CommunicationTaskStatus Status { get; set; } = CommunicationTaskStatus.New;

    public Guid? AssignedToUserId { get; set; }
    public TenantUser? AssignedToUser { get; set; }
    public DateTime? AssignedAt { get; set; }

    public DateTime? StartedAt { get; set; }
    public DateTime? CompletedAt { get; set; }

    /// <summary>Accumulated work time across all time entries.</summary>
    public long TotalTimeSpentTicks { get; set; }

    public string Title { get; set; } = string.Empty;
    public string? PreviewText { get; set; }

    /// <summary>Cached Ozon status (OPENED/CLOSED, NEW/PROCESSED, etc.).</summary>
    public string? ExternalStatus { get; set; }

    /// <summary>Ozon chat type (BUYER_SELLER / BUYER_SUPPORT / etc.). Null for non-chat tasks.</summary>
    public string? ChatType { get; set; }

    /// <summary>Number of unread messages (for chat tasks). Updated on each sync.</summary>
    public int UnreadCount { get; set; }

    public string? ExternalUrl { get; set; }

    /// <summary>Calculated on completion based on payment rate.</summary>
    public decimal? PaymentAmount { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    public bool IsDeleted { get; set; }

    public ICollection<CommunicationTaskLog> Logs { get; set; } = new List<CommunicationTaskLog>();
    public ICollection<CommunicationTimeEntry> TimeEntries { get; set; } = new List<CommunicationTimeEntry>();
}
