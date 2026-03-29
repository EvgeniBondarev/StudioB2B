namespace StudioB2B.Shared;

public record UpdateUserDto(
    string FirstName,
    string LastName,
    string? MiddleName,
    bool IsActive,
    List<Guid> Permissions);
