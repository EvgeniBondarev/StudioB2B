namespace StudioB2B.Shared;

public record MasterAuthResultDto(
    bool Success,
    string? Token = null,
    DateTime? ExpiresAt = null,
    string? Error = null);

