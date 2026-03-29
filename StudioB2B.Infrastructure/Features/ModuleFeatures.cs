using Microsoft.EntityFrameworkCore;
using StudioB2B.Domain.Entities;
using StudioB2B.Infrastructure.Persistence.Tenant;
using StudioB2B.Domain.Constants;

namespace StudioB2B.Infrastructure.Features;

public static class ModuleExtensions
{
    /// <summary>
    /// Количество производителей (для отображения на карточке модуля).
    /// </summary>
    public static Task<int> GetManufacturerCountAsync(
        this TenantDbContext db, CancellationToken ct = default)
        => db.Manufacturers.AsNoTracking().CountAsync(ct);

    /// <summary>
    /// Обеспечивает наличие всех базовых модулей в таблице TenantModules.
    /// Добавляет отсутствующие записи; существующие не трогает.
    /// </summary>
    public static async Task EnsureModulesSeededAsync(
        this TenantDbContext db, CancellationToken ct = default)
    {
        var existing = await db.TenantModules
            .Select(m => m.Code)
            .ToListAsync(ct);

        if (!existing.Contains(ModuleCodes.Manufacturers))
        {
            db.TenantModules.Add(new TenantModule
            {
                Id          = Guid.NewGuid(),
                Code        = ModuleCodes.Manufacturers,
                Name        = "Производители",
                Description = "Справочник производителей с привязкой к товарам по префиксу артикула."
            });
            await db.SaveChangesAsync(ct);
        }
    }
}

