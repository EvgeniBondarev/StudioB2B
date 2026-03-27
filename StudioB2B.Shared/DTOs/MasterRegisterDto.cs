namespace StudioB2B.Shared;

public record MasterRegisterDto(
    string Email,
    string Password,
    string FirstName,
    string LastName,
    string? MiddleName);

