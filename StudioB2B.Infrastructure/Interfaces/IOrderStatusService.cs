using StudioB2B.Domain.Entities;
using StudioB2B.Shared;

namespace StudioB2B.Infrastructure.Interfaces;

/// <summary>
/// Сервис для работы со статусами заказов тенанта.
/// Инкапсулирует все запросы к БД и скрывает DbContext от слоя UI.
/// </summary>
public interface IOrderStatusService
{
    /// <summary>
    /// Загружает начальные данные страницы: типы клиентов и счётчики для фильтр-пилюль.
    /// </summary>
    Task<OrderStatusInitData> GetInitDataAsync(CancellationToken ct = default);

    /// <summary>
    /// Постраничная выборка статусов с произвольными фильтрами, сортировкой и Dynamic LINQ.
    /// </summary>
    Task<(List<OrderStatus> Items, int TotalCount)> GetPagedAsync(
        OrderStatusPageFilter filter,
        string?               dynamicFilter,
        string?               orderBy,
        int                   skip,
        int                   take,
        CancellationToken     ct = default);

    /// <summary>Создать новый статус.</summary>
    Task<OrderStatus> CreateAsync(OrderStatus status, CancellationToken ct = default);

    /// <summary>Обновить существующий статус.</summary>
    Task UpdateAsync(OrderStatus status, CancellationToken ct = default);

    /// <summary>Мягкое удаление статуса (IsDeleted = true).</summary>
    Task SoftDeleteAsync(OrderStatus status, CancellationToken ct = default);
}

