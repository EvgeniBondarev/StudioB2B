using StudioB2B.Domain.Constants;

namespace StudioB2B.Domain.Entities;

public class CommunicationPaymentRate : IBaseEntity
{
    public Guid Id { get; set; }

    /// <summary>null = applies to all task types.</summary>
    public CommunicationTaskType? TaskType { get; set; }

    public PaymentMode PaymentMode { get; set; }

    /// <summary>null = applies to all users; specific Guid = personal rate for that user.</summary>
    public Guid? UserId { get; set; }
    public TenantUser? User { get; set; }

    /// <summary>Rubles per hour (Hourly) or rubles per task (PerTask).</summary>
    public decimal Rate { get; set; }

    /// <summary>Rate applies only when task duration >= this value (minutes). null = no minimum.</summary>
    public int? MinDurationMinutes { get; set; }

    /// <summary>Rate applies only when task duration <= this value (minutes). null = no maximum.</summary>
    public int? MaxDurationMinutes { get; set; }

    public bool IsActive { get; set; } = true;

    /// <summary>Optional label for UI (e.g. "Бонус за чат", "Ставка Иванова").</summary>
    public string? Description { get; set; }
}
