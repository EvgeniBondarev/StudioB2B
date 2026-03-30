namespace StudioB2B.Shared;

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

