using StudioB2B.Domain.Entities;

namespace StudioB2B.Shared;

/// <summary>Справочные данные для диалога создания/редактирования документа заказа.</summary>
public record TransactionEditReferenceData(
    List<OrderStatus> InternalStatuses,
    List<OrderStatus> NonTerminalStatuses,
    List<PriceType> PriceTypes,
    List<Product> Products);
