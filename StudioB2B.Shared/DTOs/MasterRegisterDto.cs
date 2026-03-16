namespace StudioB2B.Shared.DTOs;

public record MasterRegisterDto(
    string Email,
    string Password,
    string FirstName,
    string LastName,
    string? MiddleName);

