using StudioB2B.Shared;

namespace StudioB2B.Infrastructure.Interfaces;

/// <summary>
/// Aggregates Ozon product questions across all marketplace clients of the current tenant.
/// </summary>
public interface IOzonQuestionsService
{
    /// <summary>
    /// Загружает страницу вопросов по товарам с фильтрами и курсором.
    /// </summary>
    Task<OzonQuestionPageDto> GetQuestionsPageAsync(
        int pageSize = 20,
        string? cursor = null,
        DateTime? dateFrom = null,
        DateTime? dateTo = null,
        string? status = null,
        Guid? marketplaceClientId = null,
        CancellationToken ct = default);

    /// <summary>
    /// Загружает детальную информацию по вопросу (question/info + product/info/attributes).
    /// </summary>
    Task<OzonQuestionDetailDto> GetQuestionDetailAsync(
        OzonQuestionViewModelDto question,
        CancellationToken ct = default);

    /// <summary>
    /// Удаляет ответ на вопрос через /v1/question/answer/delete.
    /// Возвращает true при успехе.
    /// </summary>
    Task<bool> DeleteQuestionAnswerAsync(
        OzonQuestionViewModelDto question,
        string answerId,
        CancellationToken ct = default);

    /// <summary>
    /// Создаёт ответ на вопрос через /v1/question/answer/create.
    /// Возвращает идентификатор созданного ответа или null при ошибке.
    /// </summary>
    Task<string?> CreateQuestionAnswerAsync(
        OzonQuestionViewModelDto question,
        string text,
        CancellationToken ct = default);

    /// <summary>
    /// Меняет статус вопроса через /v1/question/change-status (Premium Plus).
    /// Возвращает true при успехе.
    /// </summary>
    Task<bool> ChangeQuestionStatusAsync(
        OzonQuestionViewModelDto question,
        string status,
        CancellationToken ct = default);

    /// <summary>
    /// Получает суммарное количество вопросов по статусам со всех маркетплейс-клиентов.
    /// </summary>
    Task<OzonQuestionCountResponseDto> GetQuestionCountsAsync(
        Guid? marketplaceClientId = null,
        CancellationToken ct = default);

    /// <summary>
    /// Получает товары с наибольшим количеством вопросов с информацией о продукте.
    /// Возвращает null, если Premium Plus недоступен.
    /// </summary>
    Task<List<OzonQuestionProductInfoDto>?> GetTopSkuProductsAsync(
        Guid? marketplaceClientId = null,
        int limit = 20,
        CancellationToken ct = default);
}

