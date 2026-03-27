namespace StudioB2B.Shared.DTOs;

public record UpdatePermissionDto(
    string                      Name,
    bool                        IsFullAccess,
    List<string>                Pages,
    List<string>                PageColumns,
    List<string>                Functions,
    List<SaveBlockedEntityDto>  BlockedEntities);
