namespace StudioB2B.Shared.DTOs;

public record TenantDto(Guid Id, string Name, string Subdomain, string ConnectionString, bool IsActive, bool IsDeleted);
