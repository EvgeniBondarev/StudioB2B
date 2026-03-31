using StudioB2B.Shared;

namespace StudioB2B.Infrastructure.Interfaces;

/// <summary>
/// Aggregates Ozon chat operations across all marketplace clients of the current tenant.
/// </summary>
public interface IOzonChatService
{
    Task<List<OzonChatViewModelDto>> GetAllChatsAsync(string? chatStatus = null, string? chatType = null, bool unreadOnly = false,
                                                      Guid? marketplaceClientId = null, CancellationToken ct = default);

    /// <summary>
    /// Загружает одну страницу чатов (pageSize штук на маркетплейс-клиент).
    /// Для каждого чата запрашивает последнее сообщение чтобы показать время.
    /// </summary>
    Task<OzonChatPageDto> GetChatsPageAsync(int pageSize = 20, string? cursor = null, string? chatStatus = null,
                                            string? chatType = null, bool unreadOnly = false, Guid? marketplaceClientId = null,
                                            bool withLastMessageInfo = true, CancellationToken ct = default);

    Task<OzonChatHistoryResponseDto?> GetChatHistoryAsync(Guid marketplaceClientId, string chatId, string direction = "Backward",
                                                       ulong? fromMessageId = null, int limit = 50, CancellationToken ct = default);

    Task<(bool Ok, string? Error)> SendMessageAsync(Guid marketplaceClientId, string chatId, string text,
                                                    CancellationToken ct = default);

    Task<(bool Ok, string? Error)> SendFileAsync(Guid marketplaceClientId, string chatId, string base64Content,
                                                 string fileName, CancellationToken ct = default);

    Task<int> MarkReadAsync(Guid marketplaceClientId, string chatId, ulong? fromMessageId = null, CancellationToken ct = default);
}


