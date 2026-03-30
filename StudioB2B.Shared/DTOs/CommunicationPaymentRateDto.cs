using StudioB2B.Domain.Constants;

namespace StudioB2B.Shared;

public class CommunicationPaymentRateDto
{
    public Guid Id { get; set; }

    public CommunicationTaskType? TaskType { get; set; }

    public PaymentMode PaymentMode { get; set; }

    public Guid? UserId { get; set; }

    public string? UserName { get; set; }

    public decimal Rate { get; set; }

    /// <summary>Rate applies when duration >= MinDurationMinutes. null = no minimum.</summary>
    public int? MinDurationMinutes { get; set; }

    /// <summary>Rate applies when duration <= MaxDurationMinutes. null = no maximum.</summary>
    public int? MaxDurationMinutes { get; set; }

    public bool IsActive { get; set; }

    public string? Description { get; set; }
}

