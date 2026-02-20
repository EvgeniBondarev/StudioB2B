namespace StudioB2B.Shared.DTOs;

public record CreateRoleRequest(string Name, string? Description, bool IsSystemRole);

