using StudioB2B.Shared.DTOs;

namespace StudioB2B.Infrastructure.Interfaces;

/// <summary>
/// Сервис применения транзакций заказов (переход статуса + изменение цен).
/// </summary>
public interface IOrderTransactionService
{
    /// <summary>Предпросмотр: правила, вычисленные значения, поля для ввода.</summary>
    Task<TransactionApplyPreview?> GetApplyPreviewAsync(Guid orderId, Guid transactionId, CancellationToken ct = default);

    /// <summary>Предпросмотр с учётом введённых пользователем значений (для динамического пересчёта формул).</summary>
    Task<TransactionApplyPreview?> GetApplyPreviewWithUserValuesAsync(Guid orderId, Guid transactionId, IReadOnlyDictionary<Guid, decimal>? userValues, CancellationToken ct = default);

    /// <summary>Контекст для расчёта (Order + UserValues) — для динамического расчёта правил на странице транзакции.</summary>
    Task<IReadOnlyDictionary<string, decimal>?> GetMergedContextAsync(Guid orderId, Guid transactionId, IReadOnlyDictionary<Guid, decimal> userValues, CancellationToken ct = default);

    /// <summary>Применить транзакцию. ruleValues — введённые пользователем значения для правил цен с UserInput. fieldRuleValues — для правил полей с UserInput (RuleId -> string Value).</summary>
    Task<TransactionApplyResult> ApplyAsync(Guid orderId, Guid transactionId, IReadOnlyDictionary<Guid, decimal>? ruleValues = null, IReadOnlyDictionary<Guid, string>? fieldRuleValues = null, CancellationToken ct = default);
}
