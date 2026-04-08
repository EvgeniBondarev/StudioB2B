namespace StudioB2B.Shared;

public record MasterRegisterResultDto(
    bool Success,
    bool RequiresVerification = false,
    string? Error = null);

