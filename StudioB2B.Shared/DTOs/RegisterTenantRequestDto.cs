namespace StudioB2B.Shared.DTOs;

public record RegisterTenantRequest(
    string CompanyName,
    string Subdomain,
    string AdminEmail,
    string AdminPassword);
