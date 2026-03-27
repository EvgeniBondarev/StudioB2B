using StudioB2B.Domain.Constants;

namespace StudioB2B.Shared.DTOs;

/// <summary>Запрос на создание/обновление правила цены документа.</summary>
public record SaveTransactionRuleRequest(
    Guid                         PriceTypeId,
    TransactionValueSourceEnum   ValueSource,
    string?                      Formula,
    Guid?                        ProductId,
    bool                         IsRequired);
