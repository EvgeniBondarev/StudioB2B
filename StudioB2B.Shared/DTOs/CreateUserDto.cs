namespace StudioB2B.Shared.DTOs;

public record CreateUserDto(
    string Email,
    string FirstName,
    string LastName,
    string? MiddleName,
    string Password,
    List<string> Roles);

