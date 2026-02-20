namespace StudioB2B.Shared.DTOs;

public record RoleDto(
    Guid Id,
    string Name,
    string? Description,
    bool IsSystemRole,
    DateTime CreatedAtUtc);

