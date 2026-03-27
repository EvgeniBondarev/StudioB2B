namespace StudioB2B.Shared;

public record UserListDto(
    Guid Id,
    string Email,
    string FirstName,
    string LastName,
    string? MiddleName,
    bool IsActive,
    List<string> Permissions);
