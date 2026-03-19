using StudioB2B.Infrastructure.Persistence.Tenant;

namespace StudioB2B.Infrastructure.Interfaces;

/// <summary>
/// Создаёт независимые экземпляры TenantDbContext для текущего тенанта.
/// Каждый вызов Create() возвращает новый контекст — это позволяет
/// избежать ошибки "second operation on same DbContext" при параллельных вызовах.
/// </summary>
public interface ITenantDbContextCreator
{
    TenantDbContext Create();
}

