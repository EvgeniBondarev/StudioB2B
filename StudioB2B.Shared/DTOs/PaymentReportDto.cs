namespace StudioB2B.Shared;

public class PaymentReportDto
{
    public List<UserTaskStatsDto> Users { get; set; } = new();

    public int TotalTasks { get; set; }

    public double TotalHours { get; set; }

    public decimal TotalPayment { get; set; }
}

