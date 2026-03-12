namespace StudioB2B.Shared.DTOs;

public record UpdateUserDto(
    string FirstName,
    string LastName,
    string? MiddleName,
    bool IsActive,
    List<string> Roles);

