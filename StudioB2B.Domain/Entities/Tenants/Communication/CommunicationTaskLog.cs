namespace StudioB2B.Domain.Entities;

public class CommunicationTaskLog : IBaseEntity
{
    public Guid Id { get; set; }

    public Guid TaskId { get; set; }
    public CommunicationTask? Task { get; set; }

    public Guid? UserId { get; set; }
    public TenantUser? User { get; set; }

    /// <summary>
    /// Created, Assigned, Started, Paused, Resumed, Released,
    /// Completed, Cancelled, MessageSent, StatusChanged, ChatHistorySnapshot.
    /// </summary>
    public string Action { get; set; } = string.Empty;

    /// <summary>JSON payload: chat history snapshot, extra context, etc.</summary>
    public string? Details { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
