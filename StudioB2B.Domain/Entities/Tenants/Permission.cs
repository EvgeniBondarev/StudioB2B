namespace StudioB2B.Domain.Entities;

/// <summary>
/// Право доступа, назначаемое пользователям тенанта.
/// </summary>
public class Permission : IBaseEntity, ISoftDelete
{
    public Guid Id { get; set; }

    /// <summary>Отображаемое название права.</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Если <c>true</c> — право даёт полный доступ ко всему
    /// (страницы, функции, колонки не проверяются).
    /// </summary>
    public bool IsFullAccess { get; set; }

    public bool IsDeleted { get; set; }

    public ICollection<PermissionPage> Pages { get; set; } = new List<PermissionPage>();
    public ICollection<PermissionPageColumn> PageColumns { get; set; } = new List<PermissionPageColumn>();
    public ICollection<PermissionFunction> Functions { get; set; } = new List<PermissionFunction>();
    public ICollection<BlockedEntity> BlockedEntities { get; set; } = new List<BlockedEntity>();
    public ICollection<TenantUserPermission> UserPermissions { get; set; } = new List<TenantUserPermission>();
}

