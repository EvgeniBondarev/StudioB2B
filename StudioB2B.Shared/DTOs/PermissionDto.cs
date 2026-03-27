namespace StudioB2B.Shared;

public record PermissionDto(
    Guid Id,
    string Name,
    bool IsFullAccess,
    List<string> Pages,
    List<string> PageColumns,
    List<string> Functions,
    List<BlockedEntityDto> BlockedEntities);
