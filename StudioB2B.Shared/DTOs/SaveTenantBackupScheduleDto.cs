namespace StudioB2B.Shared;

public class SaveTenantBackupScheduleDto
{
    public Guid TenantId { get; set; }

    public bool IsEnabled { get; set; } = true;

    public string CronExpression { get; set; } = "0 2 * * *";

    public int RetentionDays { get; set; } = 30;
}

