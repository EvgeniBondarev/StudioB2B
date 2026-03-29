namespace StudioB2B.Shared;

/// <summary>Запрос на создание или обновление документа заказа.</summary>
public record SaveOrderTransactionRequest(
    string Name,
    Guid FromSystemStatusId,
    Guid ToSystemStatusId,
    bool IsEnabled,
    string? Color,
    string? Icon,
    List<SaveTransactionRuleRequest> Rules,
    List<SaveTransactionFieldRuleRequest> FieldRules);
