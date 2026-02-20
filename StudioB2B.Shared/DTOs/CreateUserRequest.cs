namespace StudioB2B.Shared.DTOs;

public record CreateUserRequest(
    string Email,
    string FirstName,
    string LastName,
    string? MiddleName,
    string Password,
    List<string> Roles);

