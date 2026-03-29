using StudioB2B.Domain.Entities;

namespace StudioB2B.Infrastructure.Interfaces;

/// <summary>
/// Сервис для работы с правилами расчёта тенанта.
/// </summary>
public interface ICalculationRuleService
{
    /// <summary>
    /// Постраничная выборка правил для грида.
    /// </summary>
    Task<(List<CalculationRule> Items, int Total)> GetPagedAsync(
        string? filter,
        string? orderBy,
        int skip,
        int take,
        CancellationToken ct = default);

    /// <summary>
    /// Список доступных переменных для конструктора формул
    /// (имена типов цен + базовые переменные).
    /// </summary>
    Task<List<string>> GetAvailableVariablesAsync(CancellationToken ct = default);

    /// <summary>
    /// Следующий порядковый номер для нового правила.
    /// </summary>
    Task<int> GetNextSortOrderAsync(CancellationToken ct = default);

    /// <summary>
    /// Загрузить пример заказа для тестирования формулы.
    /// </summary>
    Task<OrderEntity?> GetExampleOrderAsync(string? postingNumber, CancellationToken ct = default);

    /// <summary>
    /// Все активные (не удалённые) правила — для тестирования формулы.
    /// </summary>
    Task<List<CalculationRule>> GetActiveRulesAsync(CancellationToken ct = default);

    /// <summary>
    /// Создать новое правило.
    /// </summary>
    Task<CalculationRule> CreateAsync(CalculationRule rule, CancellationToken ct = default);

    /// <summary>
    /// Обновить существующее правило.
    /// </summary>
    Task UpdateAsync(CalculationRule rule, CancellationToken ct = default);

    /// <summary>
    /// Мягкое удаление правила (IsDeleted = true).
    /// </summary>
    Task SoftDeleteAsync(CalculationRule rule, CancellationToken ct = default);
}

