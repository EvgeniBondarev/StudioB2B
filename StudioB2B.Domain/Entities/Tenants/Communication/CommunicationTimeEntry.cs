using System.ComponentModel.DataAnnotations.Schema;

namespace StudioB2B.Domain.Entities;

public class CommunicationTimeEntry : IBaseEntity
{
    public Guid Id { get; set; }

    public Guid TaskId { get; set; }
    public CommunicationTask? Task { get; set; }

    public Guid UserId { get; set; }
    public TenantUser? User { get; set; }

    public DateTime StartedAt { get; set; } = DateTime.UtcNow;

    /// <summary>Null means the timer is still running.</summary>
    public DateTime? EndedAt { get; set; }

    public string? Note { get; set; }

    [NotMapped]
    public TimeSpan Duration => EndedAt.HasValue
        ? EndedAt.Value - StartedAt
        : DateTime.UtcNow - StartedAt;
}
