namespace StudioB2B.Shared;

/// <summary>Параметры фильтрации журнала изменений.</summary>
public record AuditLogFilter(
    string? EntityName = null,
    string? ChangeType = null,
    string? UserName = null,
    DateTime? FromUtc = null,
    DateTime? ToUtc = null);
