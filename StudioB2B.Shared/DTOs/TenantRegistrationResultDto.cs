namespace StudioB2B.Shared.DTOs;

public record TenantRegistrationResultDto(
    bool Success,
    Guid? TenantId = null,
    string? Error = null);
