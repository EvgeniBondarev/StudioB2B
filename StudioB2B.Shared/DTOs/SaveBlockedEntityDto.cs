namespace StudioB2B.Shared.DTOs;

/// <summary>EntityType = string name of BlockedEntityTypeEnum.</summary>
public record SaveBlockedEntityDto(string EntityType, Guid EntityId);
