using StudioB2B.Domain.Entities;

namespace StudioB2B.Infrastructure.Interfaces;

/// <summary>
/// Сервис для работы с типами цен тенанта.
/// Инкапсулирует все запросы к БД и скрывает DbContext от слоя UI.
/// </summary>
public interface IPriceTypeService
{
    /// <summary>
    /// Постраничная выборка типов цен с Dynamic LINQ-фильтром и сортировкой.
    /// </summary>
    Task<(List<PriceType> Items, int TotalCount)> GetPagedAsync(
        string?           dynamicFilter,
        string?           orderBy,
        int               skip,
        int               take,
        CancellationToken ct = default);

    /// <summary>Создать новый пользовательский тип цены.</summary>
    Task<PriceType> CreateAsync(PriceType item, CancellationToken ct = default);

    /// <summary>
    /// Обновить Name / DeliveryScheme пользовательского типа цены.
    /// Возвращает <c>false</c>, если запись не найдена или является системной.
    /// </summary>
    Task<bool> UpdateAsync(PriceType item, CancellationToken ct = default);

    /// <summary>
    /// Мягкое удаление пользовательского типа цены (IsDeleted = true).
    /// Возвращает <c>false</c>, если запись не найдена или является системной.
    /// </summary>
    Task<bool> SoftDeleteAsync(Guid id, CancellationToken ct = default);
}

