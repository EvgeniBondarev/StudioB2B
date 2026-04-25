using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using StudioB2B.Domain.Constants;
using StudioB2B.Infrastructure.Helpers;
using StudioB2B.Infrastructure.Helpers.Http;
using StudioB2B.Infrastructure.Interfaces;
using StudioB2B.Shared;

namespace StudioB2B.Infrastructure.Services.Ozon;

public class OzonApiClient : IOzonApiClient
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly IKeyEncryptionService _encryption;
    private readonly ILogger<OzonApiClient> _logger;
    private static readonly JsonSerializerOptions OzonSerializeOptions = new() {Converters = {new UtcDateTimeJsonConverter()}};

    public OzonApiClient(IHttpClientFactory httpClientFactory, IKeyEncryptionService encryption, ILogger<OzonApiClient> logger)
    {
        _httpClientFactory = httpClientFactory;
        _encryption = encryption;
        _logger = logger;
    }

    public Task<OzonApiResultDto<OzonSellerInfoResponseDto>> GetSellerInfoAsync(string clientId, string apiKey,
                                                                             CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(clientId))
            throw new ArgumentException("ClientId must be provided.", nameof(clientId));

        if (string.IsNullOrWhiteSpace(apiKey))
            throw new ArgumentException("ApiKey must be provided.", nameof(apiKey));

        var plainApiKey = _encryption.Decrypt(apiKey);
        return SendPostAsync<OzonSellerInfoResponseDto>(OzonEndpoints.SellerInfo, clientId, plainApiKey, new { }, cancellationToken);
    }

    public Task<OzonApiResultDto<OzonFbsUnfulfilledListResponseDto>> GetFbsUnfulfilledListAsync(string clientId, string apiKey,
                                                                                             DateTime cutoffFrom, DateTime cutoffTo,
                                                                                             int limit = 100, int offset = 0,
                                                                                             CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(clientId))
            throw new ArgumentException("ClientId must be provided.", nameof(clientId));

        if (string.IsNullOrWhiteSpace(apiKey))
            throw new ArgumentException("ApiKey must be provided.", nameof(apiKey));

        var plainApiKey = _encryption.Decrypt(apiKey);
        var body = new OzonFbsUnfulfilledListRequestDto
        {
            Filter = new OzonFbsUnfulfilledFilterDto
            {
                CutoffFrom = cutoffFrom,
                CutoffTo = cutoffTo
            },
            Limit = limit,
            Offset = offset,
            With = new OzonFbsUnfulfilledWithDto { FinancialData = true }
        };

        return SendPostAsync<OzonFbsUnfulfilledListResponseDto>(OzonEndpoints.FbsUnfulfilledList, clientId, plainApiKey, body, ct);
    }

    public Task<OzonApiResultDto<OzonFboPostingListResponseDto>> GetFboPostingListAsync(string clientId, string apiKey,
                                                                                         DateTime cutoffFrom, DateTime cutoffTo,
                                                                                         int limit = 100, int offset = 0,
                                                                                         CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(clientId))
            throw new ArgumentException("ClientId must be provided.", nameof(clientId));

        if (string.IsNullOrWhiteSpace(apiKey))
            throw new ArgumentException("ApiKey must be provided.", nameof(apiKey));

        var plainApiKey = _encryption.Decrypt(apiKey);

        var body = new OzonFboPostingListRequestDto
        {
            Dir = "ASC",
            Filter = new OzonFboPostingListFilterDto
            {
                Since = cutoffFrom,
                Status = string.Empty,
                To = cutoffTo
            },
            Limit = limit,
            Offset = offset,
            With = new OzonFboPostingListWithDto
            {
                AnalyticsData = true,
                FinancialData = true,
                LegalInfo = true
            }
        };

        return SendPostAsync<OzonFboPostingListResponseDto>(OzonEndpoints.FboPostingList, clientId, plainApiKey, body, ct);
    }

    public Task<OzonApiResultDto<OzonProductPricesResponseDto>> GetProductPricesAsync(string clientId, string apiKey,
                                                                                   IReadOnlyCollection<string> offerIds,
                                                                                   string cursor = "", int limit = 100,
                                                                                   CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(clientId))
            throw new ArgumentException("ClientId must be provided.", nameof(clientId));

        if (string.IsNullOrWhiteSpace(apiKey))
            throw new ArgumentException("ApiKey must be provided.", nameof(apiKey));

        var plainApiKey = _encryption.Decrypt(apiKey);
        var body = new OzonProductPricesRequestDto
        {
            Cursor = cursor,
            Filter = new OzonProductPricesFilterDto
            {
                OfferId = offerIds.ToList(),
                Visibility = "ALL"
            },
            Limit = limit
        };

        return SendPostAsync<OzonProductPricesResponseDto>(OzonEndpoints.ProductInfoPrices, clientId, plainApiKey, body, ct);
    }

    public Task<OzonApiResultDto<OzonProductAttributesResponseDto>> GetProductAttributesAsync(string clientId, string apiKey,
                                                                                           IReadOnlyCollection<string> offerIds,
                                                                                           string lastId = "", int limit = 1000,
                                                                                           CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(clientId))
            throw new ArgumentException("ClientId must be provided.", nameof(clientId));

        if (string.IsNullOrWhiteSpace(apiKey))
            throw new ArgumentException("ApiKey must be provided.", nameof(apiKey));

        var plainApiKey = _encryption.Decrypt(apiKey);
        var body = new OzonProductAttributesRequestDto
        {
            LastId = lastId,
            Filter = new OzonProductAttributesFilterDto
            {
                OfferId = offerIds.ToList(),
                Visibility = "ALL"
            },
            Limit = limit
        };

        return SendPostAsync<OzonProductAttributesResponseDto>(OzonEndpoints.ProductInfoAttributes, clientId, plainApiKey, body, ct);
    }

    public Task<OzonApiResultDto<OzonFbsGetPostingResponseDto>> GetFbsPostingAsync(string clientId, string apiKey,
                                                                                string postingNumber, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(clientId))
            throw new ArgumentException("ClientId must be provided.", nameof(clientId));

        if (string.IsNullOrWhiteSpace(apiKey))
            throw new ArgumentException("ApiKey must be provided.", nameof(apiKey));

        var plainApiKey = _encryption.Decrypt(apiKey);
        var body = new OzonFbsGetPostingRequestDto
        {
            PostingNumber = postingNumber,
            With = new OzonFbsGetPostingWithDto()
        };

        return SendPostAsync<OzonFbsGetPostingResponseDto>(OzonEndpoints.FbsPostingGet, clientId, plainApiKey, body, ct);
    }

    public Task<OzonApiResultDto<OzonFboPostingGetResponseDto>> GetFboPostingAsync(string clientId, string apiKey,
                                                                                     string postingNumber,
                                                                                     CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(clientId))
            throw new ArgumentException("ClientId must be provided.", nameof(clientId));

        if (string.IsNullOrWhiteSpace(apiKey))
            throw new ArgumentException("ApiKey must be provided.", nameof(apiKey));

        var plainApiKey = _encryption.Decrypt(apiKey);
        var body = new OzonFboPostingGetRequestDto
        {
            PostingNumber = postingNumber,
            With = new OzonFboPostingGetWithDto
            {
                AnalyticsData = true,
                FinancialData = true,
                LegalInfo = false
            }
        };

        return SendPostAsync<OzonFboPostingGetResponseDto>(OzonEndpoints.FboPostingGet, clientId, plainApiKey, body, ct);
    }

    public Task<OzonApiResultDto<OzonReturnsListResponseDto>> GetReturnsListAsync(string clientId, string apiKey,
                                                                               OzonReturnsListRequestDto request,
                                                                               CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(clientId))
            throw new ArgumentException("ClientId must be provided.", nameof(clientId));
        if (string.IsNullOrWhiteSpace(apiKey))
            throw new ArgumentException("ApiKey must be provided.", nameof(apiKey));

        var plainApiKey = _encryption.Decrypt(apiKey);
        return SendPostAsync<OzonReturnsListResponseDto>(OzonEndpoints.ReturnsList, clientId, plainApiKey, request, ct);
    }

    public Task<OzonApiResultDto<OzonQuestionListResponseDto>> GetQuestionListAsync(string clientId, string apiKey,
                                                                                 OzonQuestionListRequestDto request,
                                                                                 CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(clientId))
            throw new ArgumentException("ClientId must be provided.", nameof(clientId));
        if (string.IsNullOrWhiteSpace(apiKey))
            throw new ArgumentException("ApiKey must be provided.", nameof(apiKey));

        var plainApiKey = _encryption.Decrypt(apiKey);
        return SendPostAsync<OzonQuestionListResponseDto>(OzonEndpoints.QuestionList, clientId, plainApiKey, request, ct);
    }

    public Task<OzonApiResultDto<OzonQuestionItemDto>> GetQuestionInfoAsync(string clientId, string apiKey,
                                                                            string questionId, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(clientId))
            throw new ArgumentException("ClientId must be provided.", nameof(clientId));
        if (string.IsNullOrWhiteSpace(apiKey))
            throw new ArgumentException("ApiKey must be provided.", nameof(apiKey));

        var plainApiKey = _encryption.Decrypt(apiKey);
        var body = new OzonQuestionInfoRequestDto { QuestionId = questionId };
        return SendPostAsync<OzonQuestionItemDto>(OzonEndpoints.QuestionInfo, clientId, plainApiKey, body, ct);
    }

    public Task<OzonApiResultDto<OzonQuestionAnswerListResponseDto>> GetQuestionAnswersAsync(
        string clientId, string apiKey, string questionId, long sku, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(clientId))
            throw new ArgumentException("ClientId must be provided.", nameof(clientId));
        if (string.IsNullOrWhiteSpace(apiKey))
            throw new ArgumentException("ApiKey must be provided.", nameof(apiKey));

        var plainApiKey = _encryption.Decrypt(apiKey);
        var body = new OzonQuestionAnswerListRequestDto { QuestionId = questionId, Sku = sku };
        return SendPostAsync<OzonQuestionAnswerListResponseDto>(
            OzonEndpoints.QuestionAnswerList, clientId, plainApiKey, body, ct);
    }

    public Task<OzonApiResultDto<OzonQuestionAnswerDeleteResponseDto>> DeleteQuestionAnswerAsync(
        string clientId, string apiKey, string answerId, long sku, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(clientId))
            throw new ArgumentException("ClientId must be provided.", nameof(clientId));
        if (string.IsNullOrWhiteSpace(apiKey))
            throw new ArgumentException("ApiKey must be provided.", nameof(apiKey));

        var plainApiKey = _encryption.Decrypt(apiKey);
        var body = new OzonQuestionAnswerDeleteRequestDto { AnswerId = answerId, Sku = sku };
        return SendPostAsync<OzonQuestionAnswerDeleteResponseDto>(
            OzonEndpoints.QuestionAnswerDelete, clientId, plainApiKey, body, ct);
    }

    public Task<OzonApiResultDto<OzonQuestionAnswerCreateResponseDto>> CreateQuestionAnswerAsync(
        string clientId, string apiKey, string questionId, long sku, string text, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(clientId))
            throw new ArgumentException("ClientId must be provided.", nameof(clientId));
        if (string.IsNullOrWhiteSpace(apiKey))
            throw new ArgumentException("ApiKey must be provided.", nameof(apiKey));

        var plainApiKey = _encryption.Decrypt(apiKey);
        var body = new OzonQuestionAnswerCreateRequestDto { QuestionId = questionId, Sku = sku, Text = text };
        return SendPostAsync<OzonQuestionAnswerCreateResponseDto>(
            OzonEndpoints.QuestionAnswerCreate, clientId, plainApiKey, body, ct);
    }

    public Task<OzonApiResultDto<OzonQuestionChangeStatusResponseDto>> ChangeQuestionStatusAsync(
        string clientId, string apiKey,
        IReadOnlyCollection<string> questionIds, string status,
        CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(clientId))
            throw new ArgumentException("ClientId must be provided.", nameof(clientId));
        if (string.IsNullOrWhiteSpace(apiKey))
            throw new ArgumentException("ApiKey must be provided.", nameof(apiKey));

        var plainApiKey = _encryption.Decrypt(apiKey);
        var body = new OzonQuestionChangeStatusRequestDto
        {
            QuestionIds = questionIds.ToList(),
            Status = status
        };
        return SendPostAsync<OzonQuestionChangeStatusResponseDto>(
            OzonEndpoints.QuestionChangeStatus, clientId, plainApiKey, body, ct);
    }

    public Task<OzonApiResultDto<OzonQuestionCountResponseDto>> GetQuestionCountAsync(
        string clientId, string apiKey, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(clientId))
            throw new ArgumentException("ClientId must be provided.", nameof(clientId));
        if (string.IsNullOrWhiteSpace(apiKey))
            throw new ArgumentException("ApiKey must be provided.", nameof(apiKey));

        var plainApiKey = _encryption.Decrypt(apiKey);
        return SendPostAsync<OzonQuestionCountResponseDto>(
            OzonEndpoints.QuestionCount, clientId, plainApiKey, new { }, ct);
    }

    public Task<OzonApiResultDto<OzonQuestionTopSkuResponseDto>> GetQuestionTopSkuAsync(
        string clientId, string apiKey, int limit = 100, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(clientId))
            throw new ArgumentException("ClientId must be provided.", nameof(clientId));
        if (string.IsNullOrWhiteSpace(apiKey))
            throw new ArgumentException("ApiKey must be provided.", nameof(apiKey));

        var plainApiKey = _encryption.Decrypt(apiKey);
        var body = new OzonQuestionTopSkuRequestDto { Limit = limit };
        return SendPostAsync<OzonQuestionTopSkuResponseDto>(
            OzonEndpoints.QuestionTopSku, clientId, plainApiKey, body, ct);
    }

    public Task<OzonApiResultDto<OzonProductAttributesResponseDto>> GetProductAttributesBySkuAsync(
        string clientId, string apiKey,
        IReadOnlyCollection<long> skus,
        CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(clientId))
            throw new ArgumentException("ClientId must be provided.", nameof(clientId));
        if (string.IsNullOrWhiteSpace(apiKey))
            throw new ArgumentException("ApiKey must be provided.", nameof(apiKey));

        var plainApiKey = _encryption.Decrypt(apiKey);
        var body = new OzonProductAttributesRequestDto
        {
            Filter = new OzonProductAttributesFilterDto
            {
                Sku = skus.Select(s => s.ToString()).ToList(),
                Visibility = "ALL"
            },
            Limit = 100
        };
        return SendPostAsync<OzonProductAttributesResponseDto>(OzonEndpoints.ProductInfoAttributes, clientId, plainApiKey, body, ct);
    }

    public Task<OzonApiResultDto<OzonProductAttributesResponseDto>> GetProductAttributesByProductIdAsync(
        string clientId, string apiKey,
        IReadOnlyCollection<long> productIds,
        CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(clientId))
            throw new ArgumentException("ClientId must be provided.", nameof(clientId));
        if (string.IsNullOrWhiteSpace(apiKey))
            throw new ArgumentException("ApiKey must be provided.", nameof(apiKey));

        var plainApiKey = _encryption.Decrypt(apiKey);
        var body = new OzonProductAttributesRequestDto
        {
            Filter = new OzonProductAttributesFilterDto
            {
                ProductId = productIds.Select(id => id.ToString()).ToList(),
                Visibility = "ALL"
            },
            Limit = 100
        };
        return SendPostAsync<OzonProductAttributesResponseDto>(OzonEndpoints.ProductInfoAttributes, clientId, plainApiKey, body, ct);
    }

    public Task<OzonApiResultDto<OzonChatListResponseDto>> GetChatListAsync(string clientId, string apiKey, OzonChatListRequestDto request,
                                                                         CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(clientId))
            throw new ArgumentException("ClientId must be provided.", nameof(clientId));
        if (string.IsNullOrWhiteSpace(apiKey))
            throw new ArgumentException("ApiKey must be provided.", nameof(apiKey));

        var plainApiKey = _encryption.Decrypt(apiKey);
        return SendPostAsync<OzonChatListResponseDto>(OzonEndpoints.ChatList, clientId, plainApiKey, request, ct);
    }

    public Task<OzonApiResultDto<OzonChatHistoryResponseDto>> GetChatHistoryAsync(string clientId, string apiKey,
                                                                               OzonChatHistoryRequestDto request,
                                                                               CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(clientId))
            throw new ArgumentException("ClientId must be provided.", nameof(clientId));
        if (string.IsNullOrWhiteSpace(apiKey))
            throw new ArgumentException("ApiKey must be provided.", nameof(apiKey));

        var plainApiKey = _encryption.Decrypt(apiKey);
        return SendPostAsync<OzonChatHistoryResponseDto>(OzonEndpoints.ChatHistory, clientId, plainApiKey, request, ct);
    }

    public Task<OzonApiResultDto<OzonSendMessageResponseDto>> SendChatMessageAsync(string clientId, string apiKey, string chatId,
                                                                                string text, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(clientId))
            throw new ArgumentException("ClientId must be provided.", nameof(clientId));
        if (string.IsNullOrWhiteSpace(apiKey))
            throw new ArgumentException("ApiKey must be provided.", nameof(apiKey));

        var plainApiKey = _encryption.Decrypt(apiKey);
        var body = new OzonSendMessageRequestDto { ChatId = chatId, Text = text };
        return SendPostAsync<OzonSendMessageResponseDto>(OzonEndpoints.ChatSendMessage, clientId, plainApiKey, body, ct);
    }

    public Task<OzonApiResultDto<OzonSendFileResponseDto>> SendChatFileAsync(string clientId, string apiKey, string chatId,
                                                                          string base64Content, string fileName,
                                                                          CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(clientId))
            throw new ArgumentException("ClientId must be provided.", nameof(clientId));
        if (string.IsNullOrWhiteSpace(apiKey))
            throw new ArgumentException("ApiKey must be provided.", nameof(apiKey));

        var plainApiKey = _encryption.Decrypt(apiKey);
        var body = new OzonSendFileRequestDto { ChatId = chatId, Base64Content = base64Content, Name = fileName };
        return SendPostAsync<OzonSendFileResponseDto>(OzonEndpoints.ChatSendFile, clientId, plainApiKey, body, ct);
    }

    public Task<OzonApiResultDto<OzonReadChatResponseDto>> ReadChatAsync(string clientId, string apiKey, string chatId,
                                                                      ulong? fromMessageId = null, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(clientId))
            throw new ArgumentException("ClientId must be provided.", nameof(clientId));
        if (string.IsNullOrWhiteSpace(apiKey))
            throw new ArgumentException("ApiKey must be provided.", nameof(apiKey));

        var plainApiKey = _encryption.Decrypt(apiKey);
        var body = new OzonReadChatRequestDto { ChatId = chatId, FromMessageId = fromMessageId };
        return SendPostAsync<OzonReadChatResponseDto>(OzonEndpoints.ChatRead, clientId, plainApiKey, body, ct);
    }

    public Task<OzonApiResultDto<OzonReviewListResponseDto>> GetReviewListAsync(
        string clientId, string apiKey, OzonReviewListRequestDto request, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(clientId))
            throw new ArgumentException("ClientId must be provided.", nameof(clientId));
        if (string.IsNullOrWhiteSpace(apiKey))
            throw new ArgumentException("ApiKey must be provided.", nameof(apiKey));

        var plainApiKey = _encryption.Decrypt(apiKey);
        return SendPostAsync<OzonReviewListResponseDto>(OzonEndpoints.ReviewList, clientId, plainApiKey, request, ct);
    }

    public Task<OzonApiResultDto<OzonReviewInfoResponseDto>> GetReviewInfoAsync(
        string clientId, string apiKey, string reviewId, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(clientId))
            throw new ArgumentException("ClientId must be provided.", nameof(clientId));
        if (string.IsNullOrWhiteSpace(apiKey))
            throw new ArgumentException("ApiKey must be provided.", nameof(apiKey));

        var plainApiKey = _encryption.Decrypt(apiKey);
        return SendPostAsync<OzonReviewInfoResponseDto>(OzonEndpoints.ReviewInfo, clientId, plainApiKey,
            new OzonReviewInfoRequestDto { ReviewId = reviewId }, ct);
    }

    public Task<OzonApiResultDto<OzonReviewCountResponseDto>> GetReviewCountAsync(
        string clientId, string apiKey, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(clientId))
            throw new ArgumentException("ClientId must be provided.", nameof(clientId));
        if (string.IsNullOrWhiteSpace(apiKey))
            throw new ArgumentException("ApiKey must be provided.", nameof(apiKey));

        var plainApiKey = _encryption.Decrypt(apiKey);
        return SendPostAsync<OzonReviewCountResponseDto>(OzonEndpoints.ReviewCount, clientId, plainApiKey, new { }, ct);
    }

    public Task<OzonApiResultDto<OzonReviewChangeStatusResponseDto>> ChangeReviewStatusAsync(
        string clientId, string apiKey, IReadOnlyCollection<string> reviewIds, string status, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(clientId))
            throw new ArgumentException("ClientId must be provided.", nameof(clientId));
        if (string.IsNullOrWhiteSpace(apiKey))
            throw new ArgumentException("ApiKey must be provided.", nameof(apiKey));

        var plainApiKey = _encryption.Decrypt(apiKey);
        return SendPostAsync<OzonReviewChangeStatusResponseDto>(OzonEndpoints.ReviewChangeStatus, clientId, plainApiKey,
            new OzonReviewChangeStatusRequestDto { ReviewIds = reviewIds.ToList(), Status = status }, ct);
    }

    public Task<OzonApiResultDto<OzonReviewCommentListResponseDto>> GetReviewCommentListAsync(
        string clientId, string apiKey, OzonReviewCommentListRequestDto request, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(clientId))
            throw new ArgumentException("ClientId must be provided.", nameof(clientId));
        if (string.IsNullOrWhiteSpace(apiKey))
            throw new ArgumentException("ApiKey must be provided.", nameof(apiKey));

        var plainApiKey = _encryption.Decrypt(apiKey);
        return SendPostAsync<OzonReviewCommentListResponseDto>(OzonEndpoints.ReviewCommentList, clientId, plainApiKey, request, ct);
    }

    public Task<OzonApiResultDto<OzonReviewCommentCreateResponseDto>> CreateReviewCommentAsync(
        string clientId, string apiKey, OzonReviewCommentCreateRequestDto request, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(clientId))
            throw new ArgumentException("ClientId must be provided.", nameof(clientId));
        if (string.IsNullOrWhiteSpace(apiKey))
            throw new ArgumentException("ApiKey must be provided.", nameof(apiKey));

        var plainApiKey = _encryption.Decrypt(apiKey);
        return SendPostAsync<OzonReviewCommentCreateResponseDto>(OzonEndpoints.ReviewCommentCreate, clientId, plainApiKey, request, ct);
    }

    public Task<OzonApiResultDto<OzonReviewCommentDeleteResponseDto>> DeleteReviewCommentAsync(
        string clientId, string apiKey, string commentId, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(clientId))
            throw new ArgumentException("ClientId must be provided.", nameof(clientId));
        if (string.IsNullOrWhiteSpace(apiKey))
            throw new ArgumentException("ApiKey must be provided.", nameof(apiKey));

        var plainApiKey = _encryption.Decrypt(apiKey);
        return SendPostAsync<OzonReviewCommentDeleteResponseDto>(OzonEndpoints.ReviewCommentDelete, clientId, plainApiKey,
            new OzonReviewCommentDeleteRequestDto { CommentId = commentId }, ct);
    }

    public async Task<(Stream? Content, string ContentType, bool Success)> DownloadChatFileAsync(string clientId, string apiKey, string fileUrl, CancellationToken ct = default)
    {
        try
        {
            var plainApiKey = _encryption.Decrypt(apiKey);
            var http = _httpClientFactory.CreateClient("Ozon");

            using var request = new HttpRequestMessage(HttpMethod.Get, fileUrl);
            request.Headers.Add("Client-Id", clientId);
            request.Headers.Add("Api-Key", plainApiKey);

            var response = await http.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, ct);
            if (!response.IsSuccessStatusCode)
                return (null, string.Empty, false);

            var contentType = response.Content.Headers.ContentType?.ToString() ?? "application/octet-stream";
            var stream = await response.Content.ReadAsStreamAsync(ct);
            return (stream, contentType, true);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error downloading chat file from {Url}", fileUrl);
            return (null, string.Empty, false);
        }
    }

    private async Task<OzonApiResultDto<TResponse>> SendPostAsync<TResponse>(string path, string clientId, string apiKey,
                                                                             object body, CancellationToken cancellationToken)
    {
        var http = _httpClientFactory.CreateClient("Ozon");

        using var request = new HttpRequestMessage(HttpMethod.Post, path);
        request.Headers.Add("Client-Id", clientId);
        request.Headers.Add("Api-Key", apiKey);

        var json = JsonSerializer.Serialize(body, OzonSerializeOptions);
        request.Content = new StringContent(json, Encoding.UTF8, "application/json");

        try
        {
            using var response = await http.SendAsync(request, cancellationToken);
            if (!response.IsSuccessStatusCode)
            {
                string? errorBody = null;
                string? ozonErrorCode = null;
                string? ozonErrorMessage = null;
                try
                {
                    errorBody = await response.Content.ReadAsStringAsync(cancellationToken);
                    if (!string.IsNullOrWhiteSpace(errorBody))
                    {
                        using var doc = JsonDocument.Parse(errorBody);
                        if (doc.RootElement.TryGetProperty("code", out var codeEl))
                            ozonErrorCode = codeEl.ValueKind == JsonValueKind.String
                                ? codeEl.GetString()
                                : codeEl.GetRawText();
                        if (doc.RootElement.TryGetProperty("message", out var msgEl))
                            ozonErrorMessage = msgEl.GetString();
                    }
                }
                catch
                {
                    // ignore read errors for error body
                }

                _logger.LogWarning("Ozon API call to {Path} failed with status {StatusCode}. Body: {Body}",
                                   path, (int)response.StatusCode, errorBody);

                var detailedMessage = !string.IsNullOrWhiteSpace(ozonErrorMessage)
                    ? ozonErrorMessage
                    : $"Ozon API call failed with status {(int)response.StatusCode}.";

                return OzonApiResultDto<TResponse>.Failure(
                    (int)response.StatusCode,
                    detailedMessage,
                    ozonErrorCode);
            }

            var data = await response.ReadFromJsonAsync<TResponse>(cancellationToken);
            return OzonApiResultDto<TResponse>.Success(data!);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            _logger.LogWarning("Ozon API call to {Path} was cancelled.", path);
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error while calling Ozon API at {Path}.", path);

            return OzonApiResultDto<TResponse>.Failure(statusCode: null, message: ex.Message);
        }
    }
}
