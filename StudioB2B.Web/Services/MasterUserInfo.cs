namespace StudioB2B.Web.Services;

public record MasterUserInfo(
    Guid UserId,
    string Email,
    string FirstName,
    string LastName,
    string? MiddleName,
    IReadOnlyList<string> Roles)
{
    public bool HasRole(string role) =>
        Roles.Any(r => string.Equals(r, role, StringComparison.OrdinalIgnoreCase));

    public bool CanCreateTenant =>
        HasRole("Admin") || HasRole("User");

    public bool IsAdmin => HasRole("Admin");
}

