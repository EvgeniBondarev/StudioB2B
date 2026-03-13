using StudioB2B.Shared.DTOs;

namespace StudioB2B.Infrastructure.Interfaces;

public interface IOzonApiClient
{
    /// <summary>
    /// Gets seller cabinet info (company, ratings, subscription) from Ozon Seller API.
    /// </summary>
    Task<OzonApiResultDto<OzonSellerInfoResponseDto>> GetSellerInfoAsync(string clientId, string apiKey,
                                                                      CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets unfulfilled FBS postings for the given cutoff window with pagination.
    /// </summary>
    Task<OzonApiResultDto<OzonFbsUnfulfilledListResponseDto>> GetFbsUnfulfilledListAsync(string clientId, string apiKey,
                                                                                      DateTime cutoffFrom, DateTime cutoffTo,
                                                                                      int limit = 100, int offset = 0,
                                                                                      CancellationToken ct = default);

    /// <summary>
    /// Gets product prices (including commissions and price indexes) for the given offer IDs.
    /// </summary>
    Task<OzonApiResultDto<OzonProductPricesResponseDto>> GetProductPricesAsync(string clientId, string apiKey,
                                                                            IReadOnlyCollection<string> offerIds, string cursor = "",
                                                                            int limit = 100, CancellationToken ct = default);

    /// <summary>
    /// Gets product attributes for the given offer IDs via /v4/product/info/attributes.
    /// </summary>
    Task<OzonApiResultDto<OzonProductAttributesResponseDto>> GetProductAttributesAsync(string clientId, string apiKey,
                                                                                    IReadOnlyCollection<string> offerIds,
                                                                                    string lastId = "", int limit = 1000,
                                                                                    CancellationToken ct = default);

    /// <summary>
    /// Gets a single FBS posting by posting number via /v3/posting/fbs/get.
    /// </summary>
    Task<OzonApiResultDto<OzonFbsGetPostingResponseDto>> GetFbsPostingAsync(string clientId, string apiKey, string postingNumber,
                                                                         CancellationToken ct = default);

    /// <summary>Gets paginated list of chats via /v3/chat/list.</summary>
    Task<OzonApiResultDto<OzonChatListResponseDto>> GetChatListAsync(string clientId, string apiKey, OzonChatListRequestDto request,
                                                                  CancellationToken ct = default);

    /// <summary>Gets chat message history via /v3/chat/history.</summary>
    Task<OzonApiResultDto<OzonChatHistoryResponseDto>> GetChatHistoryAsync(string clientId, string apiKey,
                                                                        OzonChatHistoryRequestDto request,
                                                                        CancellationToken ct = default);

    /// <summary>Sends a text message to a chat via /v1/chat/send/message.</summary>
    Task<OzonApiResultDto<OzonSendMessageResponseDto>> SendChatMessageAsync(string clientId, string apiKey, string chatId,
                                                                         string text, CancellationToken ct = default);

    /// <summary>Sends a file/image to a chat via /v1/chat/send/file.</summary>
    Task<OzonApiResultDto<OzonSendFileResponseDto>> SendChatFileAsync(string clientId, string apiKey, string chatId,
                                                                   string base64Content, string fileName,
                                                                   CancellationToken ct = default);

    /// <summary>Marks messages as read up to the given message id via /v2/chat/read.</summary>
    Task<OzonApiResultDto<OzonReadChatResponseDto>> ReadChatAsync(string clientId, string apiKey, string chatId,
                                                               ulong? fromMessageId = null, CancellationToken ct = default);

    /// <summary>Downloads a chat file/image from Ozon API URL with Client-Id/Api-Key auth.</summary>
    Task<(Stream? Content, string ContentType, bool Success)> DownloadChatFileAsync(string clientId, string apiKey,
                                                                                    string fileUrl, CancellationToken ct = default);

    /// <summary>Gets a page of returns via /v1/returns/list.</summary>
    Task<OzonApiResultDto<OzonReturnsListResponseDto>> GetReturnsListAsync(string clientId, string apiKey, OzonReturnsListRequestDto request,
                                                                        CancellationToken ct = default);
}
