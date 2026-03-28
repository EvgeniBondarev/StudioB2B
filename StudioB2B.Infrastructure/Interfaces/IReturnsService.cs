using StudioB2B.Domain.Entities;
using StudioB2B.Shared;

namespace StudioB2B.Infrastructure.Interfaces;

/// <summary>
/// Сервис для работы с возвратами тенанта на странице Returns.
/// Инкапсулирует все запросы к БД, заменяя прямое обращение к ITenantDbContextFactory из UI.
/// </summary>
public interface IReturnsService
{
    /// <summary>
    /// Загружает счётчики по типам возвратов и количество отмен, привязанных к заказу.
    /// Используется для отрисовки фильтр-чипов над таблицей.
    /// </summary>
    Task<ReturnsCountsData> GetCountsAsync(CancellationToken ct = default);

    /// <summary>
    /// Постраничный запрос возвратов со всеми фильтрами.
    /// </summary>
    Task<ReturnsPageResult> GetPageAsync(ReturnsPageRequest request, CancellationToken ct = default);
}

