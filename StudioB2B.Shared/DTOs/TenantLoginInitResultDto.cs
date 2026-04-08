namespace StudioB2B.Shared;

public record TenantLoginInitResultDto(
    bool RequiresVerification,
    string? Error = null);

