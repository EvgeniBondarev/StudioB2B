namespace StudioB2B.Shared.DTOs;

public record CreatePermissionDto(
    string Name,
    bool IsFullAccess,
    List<string> Pages,
    List<string> PageColumns,
    List<string> Functions,
    List<SaveBlockedEntityDto> BlockedEntities);

public record UpdatePermissionDto(
    string Name,
    bool IsFullAccess,
    List<string> Pages,
    List<string> PageColumns,
    List<string> Functions,
    List<SaveBlockedEntityDto> BlockedEntities);

/// <summary>EntityType = string name of BlockedEntityTypeEnum.</summary>
public record SaveBlockedEntityDto(string EntityType, Guid EntityId);

