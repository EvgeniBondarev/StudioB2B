using StudioB2B.Domain.Constants;

namespace StudioB2B.Shared;

/// <summary>One completed task row in the detailed drill-down.</summary>
public class ReportTaskItemDto
{
    public Guid Id { get; set; }

    public CommunicationTaskType TaskType { get; set; }

    public string Title { get; set; } = string.Empty;

    public string? PreviewText { get; set; }

    public string? ExternalStatus { get; set; }

    public string? ExternalUrl { get; set; }

    public string ExternalId { get; set; } = string.Empty;

    public Guid MarketplaceClientId { get; set; }

    public string MarketplaceClientName { get; set; } = string.Empty;

    public DateTime? StartedAt { get; set; }

    public DateTime? CompletedAt { get; set; }

    public long TotalTimeSpentTicks { get; set; }

    public decimal? PaymentAmount { get; set; }

    /// <summary>Строки расшифровки начисления по тарифам (текущие активные правила).</summary>
    public List<PaymentBreakdownLineDto> PaymentBreakdown { get; set; } = new();

    /// <summary>Individual time entry segments.</summary>
    public List<ReportTimeEntryDto> TimeEntries { get; set; } = new();
}


