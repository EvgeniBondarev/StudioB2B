using StudioB2B.Domain.Entities;

namespace StudioB2B.Shared;

/// <summary>Данные для инициализации страницы управления мастер-пользователями.</summary>
public record MasterUserInitData(
    List<MasterUser> Users,
    List<MasterRole> Roles,
    Dictionary<Guid, List<Guid>> UserRoleIds);
