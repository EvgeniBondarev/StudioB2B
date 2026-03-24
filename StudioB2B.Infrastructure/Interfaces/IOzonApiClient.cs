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
    /// Gets FBO posting list for the given period with pagination.
    /// Endpoint: POST /v2/posting/fbo/list
    /// </summary>
    Task<OzonApiResultDto<OzonFboPostingListResponseDto>> GetFboPostingListAsync(string clientId, string apiKey,
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

    /// <summary>
    /// Gets a single FBO posting by posting number.
    /// Endpoint: POST /v2/posting/fbo/get
    /// </summary>
    Task<OzonApiResultDto<OzonFboPostingGetResponseDto>> GetFboPostingAsync(string clientId, string apiKey, string postingNumber,
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

    /// <summary>Gets a page of product questions via /v1/question/list.</summary>
    Task<OzonApiResultDto<OzonQuestionListResponseDto>> GetQuestionListAsync(string clientId, string apiKey,
                                                                             OzonQuestionListRequestDto request,
                                                                             CancellationToken ct = default);

    /// <summary>Gets detailed info for a single question via /v1/question/info (Premium Plus).</summary>
    Task<OzonApiResultDto<OzonQuestionItemDto>> GetQuestionInfoAsync(string clientId, string apiKey,
                                                                     string questionId, CancellationToken ct = default);

    /// <summary>Gets answers for a question via /v1/question/answer/list (Premium Plus).</summary>
    Task<OzonApiResultDto<OzonQuestionAnswerListResponseDto>> GetQuestionAnswersAsync(
        string clientId, string apiKey,
        string questionId, long sku,
        CancellationToken ct = default);

    /// <summary>Deletes an answer via /v1/question/answer/delete (Premium Plus).</summary>
    Task<OzonApiResultDto<OzonQuestionAnswerDeleteResponseDto>> DeleteQuestionAnswerAsync(
        string clientId, string apiKey,
        string answerId, long sku,
        CancellationToken ct = default);

    /// <summary>Creates an answer on a question via /v1/question/answer/create (Premium Plus).</summary>
    Task<OzonApiResultDto<OzonQuestionAnswerCreateResponseDto>> CreateQuestionAnswerAsync(
        string clientId, string apiKey,
        string questionId, long sku, string text,
        CancellationToken ct = default);

    /// <summary>Changes status of questions via /v1/question/change-status (Premium Plus).</summary>
    Task<OzonApiResultDto<OzonQuestionChangeStatusResponseDto>> ChangeQuestionStatusAsync(
        string clientId, string apiKey,
        IReadOnlyCollection<string> questionIds, string status,
        CancellationToken ct = default);

    /// <summary>Gets question counts by status via /v1/question/count (Premium Plus).</summary>
    Task<OzonApiResultDto<OzonQuestionCountResponseDto>> GetQuestionCountAsync(
        string clientId, string apiKey,
        CancellationToken ct = default);

    /// <summary>Gets top SKUs by question count via /v1/question/top-sku (Premium Plus).</summary>
    Task<OzonApiResultDto<OzonQuestionTopSkuResponseDto>> GetQuestionTopSkuAsync(
        string clientId, string apiKey,
        int limit = 100,
        CancellationToken ct = default);

    /// <summary>
    /// Gets product attributes filtered by Ozon SKU list via /v4/product/info/attributes.
    /// Use when offer_id (article) is unknown but Ozon SKU is available.
    /// </summary>
    Task<OzonApiResultDto<OzonProductAttributesResponseDto>> GetProductAttributesBySkuAsync(
        string clientId, string apiKey,
        IReadOnlyCollection<long> skus,
        CancellationToken ct = default);

    /// <summary>Gets paginated list of reviews via /v1/review/list.</summary>
    Task<OzonApiResultDto<OzonReviewListResponseDto>> GetReviewListAsync(
        string clientId, string apiKey,
        OzonReviewListRequestDto request,
        CancellationToken ct = default);

    /// <summary>Gets full info for a single review via /v1/review/info.</summary>
    Task<OzonApiResultDto<OzonReviewInfoResponseDto>> GetReviewInfoAsync(
        string clientId, string apiKey,
        string reviewId,
        CancellationToken ct = default);

    /// <summary>Gets review counts by status via /v1/review/count.</summary>
    Task<OzonApiResultDto<OzonReviewCountResponseDto>> GetReviewCountAsync(
        string clientId, string apiKey,
        CancellationToken ct = default);

    /// <summary>Changes status of reviews via /v1/review/change-status.</summary>
    Task<OzonApiResultDto<OzonReviewChangeStatusResponseDto>> ChangeReviewStatusAsync(
        string clientId, string apiKey,
        IReadOnlyCollection<string> reviewIds, string status,
        CancellationToken ct = default);

    /// <summary>Gets comments for a review via /v1/review/comment/list.</summary>
    Task<OzonApiResultDto<OzonReviewCommentListResponseDto>> GetReviewCommentListAsync(
        string clientId, string apiKey,
        OzonReviewCommentListRequestDto request,
        CancellationToken ct = default);

    /// <summary>Creates a comment on a review via /v1/review/comment/create.</summary>
    Task<OzonApiResultDto<OzonReviewCommentCreateResponseDto>> CreateReviewCommentAsync(
        string clientId, string apiKey,
        OzonReviewCommentCreateRequestDto request,
        CancellationToken ct = default);

    /// <summary>Deletes a comment on a review via /v1/review/comment/delete.</summary>
    Task<OzonApiResultDto<OzonReviewCommentDeleteResponseDto>> DeleteReviewCommentAsync(
        string clientId, string apiKey,
        string commentId,
        CancellationToken ct = default);
}
