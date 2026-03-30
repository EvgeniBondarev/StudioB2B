namespace StudioB2B.Shared;

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

