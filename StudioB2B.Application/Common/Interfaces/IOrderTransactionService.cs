using StudioB2B.Application.Common;

namespace StudioB2B.Application.Common.Interfaces;

/// <summary>
/// Сервис применения транзакций заказов (переход статуса + изменение цен).
/// </summary>
public interface IOrderTransactionService
{
    /// <summary>Предпросмотр: правила, вычисленные значения, поля для ввода.</summary>
    Task<TransactionApplyPreview?> GetApplyPreviewAsync(Guid orderId, Guid transactionId, CancellationToken ct = default);

    /// <summary>Предпросмотр с учётом введённых пользователем значений (для динамического пересчёта формул).</summary>
    Task<TransactionApplyPreview?> GetApplyPreviewWithUserValuesAsync(Guid orderId, Guid transactionId, IReadOnlyDictionary<Guid, decimal> userValues, CancellationToken ct = default);

    /// <summary>Контекст для расчёта (Order + UserValues) — для динамического расчёта правил на странице транзакции.</summary>
    Task<IReadOnlyDictionary<string, decimal>?> GetMergedContextAsync(Guid orderId, Guid transactionId, IReadOnlyDictionary<Guid, decimal> userValues, CancellationToken ct = default);

    /// <summary>Применить транзакцию. ruleValues — введённые пользователем значения для правил с UserInput (RuleId -> Value).</summary>
    Task<TransactionApplyResult> ApplyAsync(Guid orderId, Guid transactionId, IReadOnlyDictionary<Guid, decimal>? ruleValues = null, CancellationToken ct = default);
}
