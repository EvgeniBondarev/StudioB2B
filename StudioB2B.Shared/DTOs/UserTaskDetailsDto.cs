namespace StudioB2B.Shared;

/// <summary>Full task breakdown for one user in a date range.</summary>
public class UserTaskDetailsDto
{
    public Guid UserId { get; set; }

    public string UserName { get; set; } = string.Empty;

    public List<ReportTaskItemDto> Tasks { get; set; } = new();

    public double TotalHours => Tasks.Sum(t => TimeSpan.FromTicks(t.TotalTimeSpentTicks).TotalHours);

    public decimal TotalPayment => Tasks.Sum(t => t.PaymentAmount ?? 0m);
}

