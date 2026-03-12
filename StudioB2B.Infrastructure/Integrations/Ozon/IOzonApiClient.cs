using StudioB2B.Infrastructure.Integrations.Ozon.Models.Chat;
using StudioB2B.Infrastructure.Integrations.Ozon.Models.FbsUnfulfilled;
using StudioB2B.Infrastructure.Integrations.Ozon.Models.ProductAttributes;
using StudioB2B.Infrastructure.Integrations.Ozon.Models.ProductPrices;
using StudioB2B.Infrastructure.Integrations.Ozon.Models.Returns;
using StudioB2B.Infrastructure.Integrations.Ozon.Models.SellerInfo;

namespace StudioB2B.Infrastructure.Integrations.Ozon;

public interface IOzonApiClient
{
    /// <summary>
    /// Gets seller cabinet info (company, ratings, subscription) from Ozon Seller API.
    /// </summary>
    Task<OzonApiResult<OzonSellerInfoResponse>> GetSellerInfoAsync(
        string clientId,
        string apiKey,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets unfulfilled FBS postings for the given cutoff window with pagination.
    /// </summary>
    Task<OzonApiResult<OzonFbsUnfulfilledListResponse>> GetFbsUnfulfilledListAsync(
        string clientId,
        string apiKey,
        DateTime cutoffFrom,
        DateTime cutoffTo,
        int limit = 100,
        int offset = 0,
        CancellationToken ct = default);

    /// <summary>
    /// Gets product prices (including commissions and price indexes) for the given offer IDs.
    /// </summary>
    Task<OzonApiResult<OzonProductPricesResponse>> GetProductPricesAsync(
        string clientId,
        string apiKey,
        IReadOnlyCollection<string> offerIds,
        string cursor = "",
        int limit = 100,
        CancellationToken ct = default);

    /// <summary>
    /// Gets product attributes for the given offer IDs via /v4/product/info/attributes.
    /// </summary>
    Task<OzonApiResult<OzonProductAttributesResponse>> GetProductAttributesAsync(
        string clientId,
        string apiKey,
        IReadOnlyCollection<string> offerIds,
        string lastId = "",
        int limit = 1000,
        CancellationToken ct = default);

    /// <summary>
    /// Gets a single FBS posting by posting number via /v3/posting/fbs/get.
    /// </summary>
    Task<OzonApiResult<OzonFbsGetPostingResponse>> GetFbsPostingAsync(
        string clientId,
        string apiKey,
        string postingNumber,
        CancellationToken ct = default);

    /// <summary>Gets paginated list of chats via /v3/chat/list.</summary>
    Task<OzonApiResult<OzonChatListResponse>> GetChatListAsync(
        string clientId,
        string apiKey,
        OzonChatListRequest request,
        CancellationToken ct = default);

    /// <summary>Gets chat message history via /v3/chat/history.</summary>
    Task<OzonApiResult<OzonChatHistoryResponse>> GetChatHistoryAsync(
        string clientId,
        string apiKey,
        OzonChatHistoryRequest request,
        CancellationToken ct = default);

    /// <summary>Sends a text message to a chat via /v1/chat/send/message.</summary>
    Task<OzonApiResult<OzonSendMessageResponse>> SendChatMessageAsync(
        string clientId,
        string apiKey,
        string chatId,
        string text,
        CancellationToken ct = default);

    /// <summary>Sends a file/image to a chat via /v1/chat/send/file.</summary>
    Task<OzonApiResult<OzonSendFileResponse>> SendChatFileAsync(
        string clientId,
        string apiKey,
        string chatId,
        string base64Content,
        string fileName,
        CancellationToken ct = default);

    /// <summary>Marks messages as read up to the given message id via /v2/chat/read.</summary>
    Task<OzonApiResult<OzonReadChatResponse>> ReadChatAsync(
        string clientId,
        string apiKey,
        string chatId,
        ulong? fromMessageId = null,
        CancellationToken ct = default);

    /// <summary>Downloads a chat file/image from Ozon API URL with Client-Id/Api-Key auth.</summary>
    Task<(Stream? Content, string ContentType, bool Success)> DownloadChatFileAsync(
        string clientId,
        string apiKey,
        string fileUrl,
        CancellationToken ct = default);

    // ── Returns ──────────────────────────────────────────────────────────────

    /// <summary>Gets a page of returns via /v1/returns/list.</summary>
    Task<OzonApiResult<OzonReturnsListResponse>> GetReturnsListAsync(
        string clientId,
        string apiKey,
        OzonReturnsListRequest request,
        CancellationToken ct = default);
}
