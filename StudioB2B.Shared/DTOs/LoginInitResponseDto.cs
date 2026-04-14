namespace StudioB2B.Shared;

public record LoginInitResponseDto(bool RequiresVerification, string? Token = null, DateTime? ExpiresAt = null);

