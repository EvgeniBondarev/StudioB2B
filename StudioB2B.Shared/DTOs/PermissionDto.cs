namespace StudioB2B.Shared.DTOs;

public record PermissionDto(
    Guid Id,
    string Name,
    bool IsFullAccess,
    List<string> Pages,
    List<string> PageColumns,
    List<string> Functions,
    List<BlockedEntityDto> BlockedEntities);

public record BlockedEntityDto(Guid Id, string EntityType, Guid EntityId);

