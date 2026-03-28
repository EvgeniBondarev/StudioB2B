namespace StudioB2B.Shared;

public record TenantRegistrationResultDto(
    bool Success,
    Guid? TenantId = null,
    string? Error = null);
