using StudioB2B.Domain.Entities;

namespace StudioB2B.Shared.DTOs;

/// <summary>Данные для панели массового изменения статуса.</summary>
public record OrderSelectionInfo(
    List<Guid>             StatusIds,
    bool                   HasNullStatus,
    List<OrderTransaction> AvailableTransactions);
