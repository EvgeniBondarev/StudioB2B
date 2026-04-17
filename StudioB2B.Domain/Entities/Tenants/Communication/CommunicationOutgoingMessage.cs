using StudioB2B.Domain.Constants;

namespace StudioB2B.Domain.Entities;

/// <summary>
/// Tracks which internal employee sent each outgoing chat message, question answer, or review comment.
/// Keyed by (ExternalId, TaskType, ExternalMessageId).
/// </summary>
public class CommunicationOutgoingMessage
{
    public Guid Id { get; set; }

    /// <summary>ChatId / QuestionId / ReviewId from Ozon.</summary>
    public string ExternalId { get; set; } = string.Empty;

    public CommunicationTaskType TaskType { get; set; }

    /// <summary>
    /// Ozon message ID (ulong as string for chats), answerId for questions, commentId for reviews.
    /// </summary>
    public string ExternalMessageId { get; set; } = string.Empty;

    public Guid SentByUserId { get; set; }

    public TenantUser? SentByUser { get; set; }

    /// <summary>Snapshot of the employee display name at the time of sending.</summary>
    public string SentByUserName { get; set; } = string.Empty;

    public DateTime SentAt { get; set; } = DateTime.UtcNow;
}

