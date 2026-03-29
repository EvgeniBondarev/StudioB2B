using StudioB2B.Domain.Constants;
using StudioB2B.Domain.Entities;
using StudioB2B.Shared;

namespace StudioB2B.Infrastructure.Interfaces;

/// <summary>
/// Сервис проведения документов заказов (переход статуса + изменение цен).
/// </summary>
public interface IOrderTransactionService
{
    /// <summary>Загрузить заказы для страницы проведения документа (со всеми нужными Include).</summary>
    Task<List<OrderEntity>> GetOrdersForApplyAsync(IEnumerable<Guid> orderIds, CancellationToken ct = default);

    /// <summary>Загрузить доступные документы для заданного системного статуса.</summary>
    Task<List<OrderTransaction>> GetTransactionsForStatusAsync(Guid statusId, CancellationToken ct = default);

    /// <summary>
    /// Загрузить данные для инициализации диалога выбора документа:
    /// название статуса и список доступных документов.
    /// </summary>
    Task<(string? StatusName, List<OrderTransaction> Transactions)> GetApplyDialogInitDataAsync(
        Guid statusId, CancellationToken ct = default);

    /// <summary>Загрузить справочные данные, необходимые для полей ввода документа.</summary>
    Task<TransactionReferenceData> GetReferenceDataAsync(IEnumerable<FieldReferenceTypeEnum> refTypes, CancellationToken ct = default);

    /// <summary>Предпросмотр: правила, вычисленные значения, поля для ввода.</summary>
    Task<TransactionApplyPreviewDto?> GetApplyPreviewAsync(Guid orderId, Guid transactionId, CancellationToken ct = default);

    /// <summary>Предпросмотр с учётом введённых пользователем значений (для динамического пересчёта формул).</summary>
    Task<TransactionApplyPreviewDto?> GetApplyPreviewWithUserValuesAsync(Guid orderId, Guid transactionId, IReadOnlyDictionary<Guid, decimal>? userValues, CancellationToken ct = default);

    /// <summary>Контекст для расчёта (Order + UserValues) — для динамического расчёта правил на странице проведения.</summary>
    Task<IReadOnlyDictionary<string, decimal>?> GetMergedContextAsync(Guid orderId, Guid transactionId, IReadOnlyDictionary<Guid, decimal> userValues, CancellationToken ct = default);

    /// <summary>Провести документ. ruleValues — введённые пользователем значения для правил цен с UserInput. fieldRuleValues — для правил полей с UserInput (RuleId -> string Value).</summary>
    Task<TransactionApplyResultDto> ApplyAsync(Guid orderId, Guid transactionId, IReadOnlyDictionary<Guid, decimal>? ruleValues = null, IReadOnlyDictionary<Guid, string>? fieldRuleValues = null, CancellationToken ct = default);
}
