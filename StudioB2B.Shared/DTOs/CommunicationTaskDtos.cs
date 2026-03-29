using StudioB2B.Domain.Constants;

namespace StudioB2B.Shared.DTOs;

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
}

public class CommunicationTaskDetailDto : CommunicationTaskDto
{
    public List<CommunicationTaskLogDto> Logs { get; set; } = new();
    public List<CommunicationTimeEntryDto> TimeEntries { get; set; } = new();
}

public class CommunicationTaskLogDto
{
    public Guid Id { get; set; }
    public Guid? UserId { get; set; }
    public string? UserName { get; set; }
    public string Action { get; set; } = string.Empty;
    public string? Details { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class CommunicationTimeEntryDto
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public string UserName { get; set; } = string.Empty;
    public DateTime StartedAt { get; set; }
    public DateTime? EndedAt { get; set; }
    public string? Note { get; set; }
    public double DurationMinutes { get; set; }
}

public class CommunicationTaskFilter
{
    public CommunicationTaskType? TaskType { get; set; }

    public CommunicationTaskStatus? Status { get; set; }

    public Guid? AssignedToUserId { get; set; }

    public Guid? MarketplaceClientId { get; set; }

    public DateTime? From { get; set; }

    public DateTime? To { get; set; }

    /// <summary>How many Done/Cancelled tasks to return in GetBoardAsync.</summary>
    public int DoneTake { get; set; } = 25;
}

public class UserTaskStatsDto
{
    public Guid UserId { get; set; }
    public string UserName { get; set; } = string.Empty;

    public int ChatsDone { get; set; }
    public int QuestionsDone { get; set; }
    public int ReviewsDone { get; set; }
    public int TotalDone => ChatsDone + QuestionsDone + ReviewsDone;

    public double TotalHours { get; set; }
    public decimal TotalPayment { get; set; }
}

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
    /// <summary>Individual time entry segments.</summary>
    public List<ReportTimeEntryDto> TimeEntries { get; set; } = new();
}

public class ReportTimeEntryDto
{
    public DateTime StartedAt { get; set; }
    public DateTime? EndedAt { get; set; }
    public double DurationMinutes { get; set; }
    public string? Note { get; set; }
}

/// <summary>Full task breakdown for one user in a date range.</summary>
public class UserTaskDetailsDto
{
    public Guid UserId { get; set; }
    public string UserName { get; set; } = string.Empty;
    public List<ReportTaskItemDto> Tasks { get; set; } = new();
    public double TotalHours => Tasks.Sum(t => TimeSpan.FromTicks(t.TotalTimeSpentTicks).TotalHours);
    public decimal TotalPayment => Tasks.Sum(t => t.PaymentAmount ?? 0m);
}

public class DailyActivityDto
{
    public DateTime Date { get; set; }
    public int ChatsDone { get; set; }
    public int QuestionsDone { get; set; }
    public int ReviewsDone { get; set; }
    public int TotalDone => ChatsDone + QuestionsDone + ReviewsDone;
    public double TotalHours { get; set; }
    public decimal TotalPayment { get; set; }
    /// <summary>Formatted label for chart axis.</summary>
    public string DateStr => Date.ToString("dd.MM");
}

public class PersonalStatsDto
{
    public Guid UserId { get; set; }
    public string UserName { get; set; } = string.Empty;
    public int ChatsDone { get; set; }
    public int QuestionsDone { get; set; }
    public int ReviewsDone { get; set; }
    public int TotalDone => ChatsDone + QuestionsDone + ReviewsDone;
    public double TotalHours { get; set; }
    public decimal TotalPayment { get; set; }
    public List<DailyActivityDto> DailyActivity { get; set; } = new();
    public List<ReportTaskItemDto> RecentTasks { get; set; } = new();
}

public class PaymentReportDto
{
    public List<UserTaskStatsDto> Users { get; set; } = new();
    public int TotalTasks { get; set; }
    public double TotalHours { get; set; }
    public decimal TotalPayment { get; set; }
}

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

public class TaskBoardDto
{
    public List<CommunicationTaskDto> NewTasks { get; set; } = new();

    public List<CommunicationTaskDto> InProgressTasks { get; set; } = new();

    public List<CommunicationTaskDto> DoneTasks { get; set; } = new();

    /// <summary>Total Done/Cancelled count in the DB (for infinite scroll).</summary>
    public int DoneTotalCount { get; set; }

    public Dictionary<CommunicationTaskType, int> TypeCounts { get; set; } = new();

    /// <summary>Estimated flat PerTask payment per task type (global rates, no min duration).</summary>
    public Dictionary<CommunicationTaskType, decimal> PaymentEstimates { get; set; } = new();

    /// <summary>Hourly rate per task type (global rates, no min duration).</summary>
    public Dictionary<CommunicationTaskType, decimal> HourlyEstimates { get; set; } = new();
}
