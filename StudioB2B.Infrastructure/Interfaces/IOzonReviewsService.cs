using StudioB2B.Shared.DTOs;

namespace StudioB2B.Infrastructure.Interfaces;

/// <summary>
/// Aggregates Ozon reviews across all marketplace clients of the current tenant.
/// </summary>
public interface IOzonReviewsService
{
    /// <summary>Загружает страницу отзывов с фильтром по статусу и маркетплейс-клиенту.</summary>
    Task<OzonReviewPageDto> GetReviewsPageAsync(
        int pageSize = 20,
        string? cursor = null,
        string? status = null,
        Guid? marketplaceClientId = null,
        CancellationToken ct = default);

    /// <summary>Загружает детальную информацию по отзыву (info + комментарии).</summary>
    Task<OzonReviewDetailDto> GetReviewDetailAsync(
        OzonReviewViewModelDto review,
        CancellationToken ct = default);

    /// <summary>Меняет статус одного отзыва. Возвращает true при успехе.</summary>
    Task<bool> ChangeReviewStatusAsync(
        OzonReviewViewModelDto review,
        string status,
        CancellationToken ct = default);

    /// <summary>Суммарное количество отзывов по статусам.</summary>
    Task<OzonReviewCountAggregateDto> GetReviewCountsAsync(
        Guid? marketplaceClientId = null,
        CancellationToken ct = default);

    /// <summary>
    /// Создаёт комментарий на отзыв.
    /// Возвращает идентификатор созданного комментария или null при ошибке.
    /// </summary>
    Task<string?> CreateReviewCommentAsync(
        OzonReviewViewModelDto review,
        string text,
        bool markAsProcessed = true,
        CancellationToken ct = default);

    /// <summary>Удаляет комментарий на отзыв. Возвращает true при успехе.</summary>
    Task<bool> DeleteReviewCommentAsync(
        OzonReviewViewModelDto review,
        string commentId,
        CancellationToken ct = default);
}

