namespace StudioB2B.Shared;

public class TenantBackupScheduleDto
{
    public Guid Id { get; set; }

    public Guid TenantId { get; set; }

    public bool IsEnabled { get; set; }

    public string CronExpression { get; set; } = "0 2 * * *";

    public int RetentionDays { get; set; } = 30;

    public string? HangfireJobId { get; set; }

    public DateTime UpdatedAtUtc { get; set; }
}

