namespace StudioB2B.Shared;
public class TenantRestoreHistoryDto
{
    public Guid Id { get; set; }
    public Guid TenantId { get; set; }
    public string SourceObjectKey { get; set; } = "";
    public string SourceType { get; set; } = "";
    public string Status { get; set; } = "";
    public string? ErrorMessage { get; set; }
    public DateTime StartedAtUtc { get; set; }
    public DateTime? CompletedAtUtc { get; set; }
}
