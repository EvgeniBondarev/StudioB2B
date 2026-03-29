using StudioB2B.Domain.Constants;

namespace StudioB2B.Shared;

/// <summary>Запрос на создание/обновление правила поля документа.</summary>
public record SaveTransactionFieldRuleRequest(
    string EntityPath,
    TransactionFieldValueSourceEnum ValueSource,
    string? FixedValue,
    bool IsRequired);
