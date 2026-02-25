namespace StudioB2B.Web.Models;

public record LoginRequest(string Email, string Password);

public record RegisterRequest(string Email, string Password);

public record RegisterTenantRequest(
    string CompanyName,
    string Subdomain,
    string AdminEmail,
    string AdminPassword);

