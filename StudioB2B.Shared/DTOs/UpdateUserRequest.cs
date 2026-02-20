namespace StudioB2B.Shared.DTOs;

public record UpdateUserRequest(
    string FirstName,
    string LastName,
    string? MiddleName,
    bool IsActive,
    List<string> Roles);

