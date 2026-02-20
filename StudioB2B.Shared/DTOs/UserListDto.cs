namespace StudioB2B.Shared.DTOs;

public record UserListDto(
    Guid Id,
    string Email,
    string FirstName,
    string LastName,
    string? MiddleName,
    bool IsActive,
    DateTime CreatedAtUtc,
    DateTime? LastLoginAtUtc,
    List<string> Roles);

