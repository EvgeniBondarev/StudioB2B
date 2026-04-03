namespace StudioB2B.Shared;

public class TenantBackupHistoryDto
{
    public Guid Id { get; set; }

    public Guid TenantId { get; set; }

    public string? MinioObjectKey { get; set; }

    public long? SizeBytes { get; set; }

    public string Status { get; set; } = "";

    public string? ErrorMessage { get; set; }

    public DateTime StartedAtUtc { get; set; }

    public DateTime? CompletedAtUtc { get; set; }
}

