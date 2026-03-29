namespace StudioB2B.Shared;

public record CreateUserDto(
    string Email,
    string FirstName,
    string LastName,
    string? MiddleName,
    string Password,
    List<Guid> Permissions);
