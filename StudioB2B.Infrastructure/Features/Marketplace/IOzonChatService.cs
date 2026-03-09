using StudioB2B.Infrastructure.Integrations.Ozon.Models.Chat;

namespace StudioB2B.Infrastructure.Features.Marketplace;

/// <summary>
/// Aggregates Ozon chat operations across all marketplace clients of the current tenant.
/// </summary>
public interface IOzonChatService
{
    Task<List<OzonChatViewModel>> GetAllChatsAsync(
        string? chatStatus = null,
        string? chatType = null,
        bool unreadOnly = false,
        Guid? marketplaceClientId = null,
        CancellationToken ct = default);

    /// <summary>
    /// Загружает одну страницу чатов (pageSize штук на маркетплейс-клиент).
    /// Для каждого чата запрашивает последнее сообщение чтобы показать время.
    /// </summary>
    Task<OzonChatPage> GetChatsPageAsync(
        int pageSize = 20,
        string? cursor = null,
        string? chatStatus = null,
        string? chatType = null,
        bool unreadOnly = false,
        Guid? marketplaceClientId = null,
        CancellationToken ct = default);

    Task<OzonChatHistoryResponse?> GetChatHistoryAsync(
        Guid marketplaceClientId,
        string chatId,
        string direction = "Backward",
        ulong? fromMessageId = null,
        int limit = 50,
        CancellationToken ct = default);

    Task<(bool Ok, string? Error)> SendMessageAsync(
        Guid marketplaceClientId,
        string chatId,
        string text,
        CancellationToken ct = default);

    Task<(bool Ok, string? Error)> SendFileAsync(
        Guid marketplaceClientId,
        string chatId,
        string base64Content,
        string fileName,
        CancellationToken ct = default);

    Task<int> MarkReadAsync(
        Guid marketplaceClientId,
        string chatId,
        ulong? fromMessageId = null,
        CancellationToken ct = default);
}

/// <summary>
/// A flattened view model combining an Ozon chat item with its marketplace client info.
/// </summary>
public class OzonChatViewModel
{
    public Guid MarketplaceClientId { get; set; }
    public string MarketplaceClientName { get; set; } = string.Empty;

    // Stored encrypted — decrypted only when calling API
    internal string ApiId { get; set; } = string.Empty;
    internal string ApiKey { get; set; } = string.Empty;

    public string ChatId { get; set; } = string.Empty;
    public string ChatStatus { get; set; } = string.Empty;
    public string ChatType { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }

    public ulong? FirstUnreadMessageId { get; set; }
    public ulong? LastMessageId { get; set; }
    public int UnreadCount { get; set; }

    /// <summary>
    /// Время последнего сообщения. Заполняется после загрузки истории чата.
    /// До открытия чата совпадает с CreatedAt.
    /// </summary>
    public DateTime LastMessageAt { get; set; }
}

/// <summary>Результат постраничной загрузки чатов.</summary>
public class OzonChatPage
{
    public List<OzonChatViewModel> Chats { get; set; } = new();
    /// <summary>Курсор для следующей страницы. null — страниц больше нет.</summary>
    public string? NextCursor { get; set; }
    public bool HasMore => NextCursor is not null;
}

