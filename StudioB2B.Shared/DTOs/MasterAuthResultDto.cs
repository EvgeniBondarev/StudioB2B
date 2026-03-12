namespace StudioB2B.Shared.DTOs;

public record MasterAuthResultDto(
    bool Success,
    string? Token = null,
    DateTime? ExpiresAt = null,
    string? Error = null);

