using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using StudioB2B.Infrastructure.Http;
using StudioB2B.Infrastructure.Integrations.Ozon.Models.Chat;
using StudioB2B.Infrastructure.Integrations.Ozon.Models.FbsUnfulfilled;
using StudioB2B.Infrastructure.Integrations.Ozon.Models.ProductAttributes;
using StudioB2B.Infrastructure.Integrations.Ozon.Models.ProductPrices;
using StudioB2B.Infrastructure.Integrations.Ozon.Models.Returns;
using StudioB2B.Infrastructure.Integrations.Ozon.Models.SellerInfo;
using StudioB2B.Infrastructure.Interfaces;

namespace StudioB2B.Infrastructure.Integrations.Ozon;

public class OzonApiClient : IOzonApiClient
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly IKeyEncryptionService _encryption;
    private readonly ILogger<OzonApiClient> _logger;

    public OzonApiClient(
        IHttpClientFactory httpClientFactory,
        IKeyEncryptionService encryption,
        ILogger<OzonApiClient> logger)
    {
        _httpClientFactory = httpClientFactory;
        _encryption = encryption;
        _logger = logger;
    }

    public Task<OzonApiResult<OzonSellerInfoResponse>> GetSellerInfoAsync(
        string clientId,
        string apiKey,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(clientId))
            throw new ArgumentException("ClientId must be provided.", nameof(clientId));

        if (string.IsNullOrWhiteSpace(apiKey))
            throw new ArgumentException("ApiKey must be provided.", nameof(apiKey));

        var plainApiKey = _encryption.Decrypt(apiKey);

        return SendPostAsync<OzonSellerInfoResponse>(
            OzonEndpoints.SellerInfo,
            clientId,
            plainApiKey,
            new { },
            cancellationToken);
    }

    public Task<OzonApiResult<OzonFbsUnfulfilledListResponse>> GetFbsUnfulfilledListAsync(
        string clientId,
        string apiKey,
        DateTime cutoffFrom,
        DateTime cutoffTo,
        int limit = 100,
        int offset = 0,
        CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(clientId))
            throw new ArgumentException("ClientId must be provided.", nameof(clientId));

        if (string.IsNullOrWhiteSpace(apiKey))
            throw new ArgumentException("ApiKey must be provided.", nameof(apiKey));

        var plainApiKey = _encryption.Decrypt(apiKey);

        var body = new OzonFbsUnfulfilledListRequest
        {
            Filter = new OzonFbsUnfulfilledFilter
            {
                CutoffFrom = cutoffFrom,
                CutoffTo = cutoffTo
            },
            Limit = limit,
            Offset = offset,
            With = new OzonFbsUnfulfilledWith { FinancialData = true }
        };

        return SendPostAsync<OzonFbsUnfulfilledListResponse>(
            OzonEndpoints.FbsUnfulfilledList,
            clientId,
            plainApiKey,
            body,
            ct);
    }

    public Task<OzonApiResult<OzonProductPricesResponse>> GetProductPricesAsync(
        string clientId,
        string apiKey,
        IReadOnlyCollection<string> offerIds,
        string cursor = "",
        int limit = 100,
        CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(clientId))
            throw new ArgumentException("ClientId must be provided.", nameof(clientId));

        if (string.IsNullOrWhiteSpace(apiKey))
            throw new ArgumentException("ApiKey must be provided.", nameof(apiKey));

        var plainApiKey = _encryption.Decrypt(apiKey);

        var body = new OzonProductPricesRequest
        {
            Cursor = cursor,
            Filter = new OzonProductPricesFilter
            {
                OfferId = offerIds.ToList(),
                Visibility = "ALL"
            },
            Limit = limit
        };

        return SendPostAsync<OzonProductPricesResponse>(
            OzonEndpoints.ProductInfoPrices,
            clientId,
            plainApiKey,
            body,
            ct);
    }

    public Task<OzonApiResult<OzonProductAttributesResponse>> GetProductAttributesAsync(
        string clientId,
        string apiKey,
        IReadOnlyCollection<string> offerIds,
        string lastId = "",
        int limit = 1000,
        CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(clientId))
            throw new ArgumentException("ClientId must be provided.", nameof(clientId));

        if (string.IsNullOrWhiteSpace(apiKey))
            throw new ArgumentException("ApiKey must be provided.", nameof(apiKey));

        var plainApiKey = _encryption.Decrypt(apiKey);

        var body = new OzonProductAttributesRequest
        {
            LastId = lastId,
            Filter = new OzonProductAttributesFilter
            {
                OfferId = offerIds.ToList(),
                Visibility = "ALL"
            },
            Limit = limit
        };

        return SendPostAsync<OzonProductAttributesResponse>(
            OzonEndpoints.ProductInfoAttributes,
            clientId,
            plainApiKey,
            body,
            ct);
    }

    public Task<OzonApiResult<OzonFbsGetPostingResponse>> GetFbsPostingAsync(
        string clientId,
        string apiKey,
        string postingNumber,
        CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(clientId))
            throw new ArgumentException("ClientId must be provided.", nameof(clientId));

        if (string.IsNullOrWhiteSpace(apiKey))
            throw new ArgumentException("ApiKey must be provided.", nameof(apiKey));

        var plainApiKey = _encryption.Decrypt(apiKey);

        var body = new OzonFbsGetPostingRequest
        {
            PostingNumber = postingNumber,
            With = new OzonFbsGetPostingWith()
        };

        return SendPostAsync<OzonFbsGetPostingResponse>(
            OzonEndpoints.FbsPostingGet,
            clientId,
            plainApiKey,
            body,
            ct);
    }

    // ── Returns ──────────────────────────────────────────────────────────────

    public Task<OzonApiResult<OzonReturnsListResponse>> GetReturnsListAsync(
        string clientId,
        string apiKey,
        OzonReturnsListRequest request,
        CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(clientId))
            throw new ArgumentException("ClientId must be provided.", nameof(clientId));
        if (string.IsNullOrWhiteSpace(apiKey))
            throw new ArgumentException("ApiKey must be provided.", nameof(apiKey));

        var plainApiKey = _encryption.Decrypt(apiKey);
        return SendPostAsync<OzonReturnsListResponse>(OzonEndpoints.ReturnsList, clientId, plainApiKey, request, ct);
    }

    // ── Chat ─────────────────────────────────────────────────────────────────

    public Task<OzonApiResult<OzonChatListResponse>> GetChatListAsync(
        string clientId,
        string apiKey,
        OzonChatListRequest request,
        CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(clientId))
            throw new ArgumentException("ClientId must be provided.", nameof(clientId));
        if (string.IsNullOrWhiteSpace(apiKey))
            throw new ArgumentException("ApiKey must be provided.", nameof(apiKey));

        var plainApiKey = _encryption.Decrypt(apiKey);
        return SendPostAsync<OzonChatListResponse>(OzonEndpoints.ChatList, clientId, plainApiKey, request, ct);
    }

    public Task<OzonApiResult<OzonChatHistoryResponse>> GetChatHistoryAsync(
        string clientId,
        string apiKey,
        OzonChatHistoryRequest request,
        CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(clientId))
            throw new ArgumentException("ClientId must be provided.", nameof(clientId));
        if (string.IsNullOrWhiteSpace(apiKey))
            throw new ArgumentException("ApiKey must be provided.", nameof(apiKey));

        var plainApiKey = _encryption.Decrypt(apiKey);
        return SendPostAsync<OzonChatHistoryResponse>(OzonEndpoints.ChatHistory, clientId, plainApiKey, request, ct);
    }

    public Task<OzonApiResult<OzonSendMessageResponse>> SendChatMessageAsync(
        string clientId,
        string apiKey,
        string chatId,
        string text,
        CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(clientId))
            throw new ArgumentException("ClientId must be provided.", nameof(clientId));
        if (string.IsNullOrWhiteSpace(apiKey))
            throw new ArgumentException("ApiKey must be provided.", nameof(apiKey));

        var plainApiKey = _encryption.Decrypt(apiKey);
        var body = new OzonSendMessageRequest { ChatId = chatId, Text = text };
        return SendPostAsync<OzonSendMessageResponse>(OzonEndpoints.ChatSendMessage, clientId, plainApiKey, body, ct);
    }

    public Task<OzonApiResult<OzonSendFileResponse>> SendChatFileAsync(
        string clientId,
        string apiKey,
        string chatId,
        string base64Content,
        string fileName,
        CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(clientId))
            throw new ArgumentException("ClientId must be provided.", nameof(clientId));
        if (string.IsNullOrWhiteSpace(apiKey))
            throw new ArgumentException("ApiKey must be provided.", nameof(apiKey));

        var plainApiKey = _encryption.Decrypt(apiKey);
        var body = new OzonSendFileRequest { ChatId = chatId, Base64Content = base64Content, Name = fileName };
        return SendPostAsync<OzonSendFileResponse>(OzonEndpoints.ChatSendFile, clientId, plainApiKey, body, ct);
    }

    public Task<OzonApiResult<OzonReadChatResponse>> ReadChatAsync(
        string clientId,
        string apiKey,
        string chatId,
        ulong? fromMessageId = null,
        CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(clientId))
            throw new ArgumentException("ClientId must be provided.", nameof(clientId));
        if (string.IsNullOrWhiteSpace(apiKey))
            throw new ArgumentException("ApiKey must be provided.", nameof(apiKey));

        var plainApiKey = _encryption.Decrypt(apiKey);
        var body = new OzonReadChatRequest { ChatId = chatId, FromMessageId = fromMessageId };
        return SendPostAsync<OzonReadChatResponse>(OzonEndpoints.ChatRead, clientId, plainApiKey, body, ct);
    }

    public async Task<(Stream? Content, string ContentType, bool Success)> DownloadChatFileAsync(
        string clientId,
        string apiKey,
        string fileUrl,
        CancellationToken ct = default)
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

    private static readonly JsonSerializerOptions _ozonSerializeOptions = new()
    {
        Converters = { new UtcDateTimeJsonConverter() }
    };

    private async Task<OzonApiResult<TResponse>> SendPostAsync<TResponse>(
        string path,
        string clientId,
        string apiKey,
        object body,
        CancellationToken cancellationToken)
    {
        var http = _httpClientFactory.CreateClient("Ozon");

        using var request = new HttpRequestMessage(HttpMethod.Post, path);
        request.Headers.Add("Client-Id", clientId);
        request.Headers.Add("Api-Key", apiKey);

        var json = JsonSerializer.Serialize(body, _ozonSerializeOptions);
        request.Content = new StringContent(json, Encoding.UTF8, "application/json");

        try
        {
            using var response = await http.SendAsync(request, cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                string? errorBody = null;
                try
                {
                    errorBody = await response.Content.ReadAsStringAsync(cancellationToken);
                }
                catch
                {
                    // ignore read errors for error body
                }

                _logger.LogWarning(
                    "Ozon API call to {Path} failed with status {StatusCode}. Body: {Body}",
                    path,
                    (int)response.StatusCode,
                    errorBody);

                return OzonApiResult<TResponse>.Failure(
                    (int)response.StatusCode,
                    $"Ozon API call failed with status {(int)response.StatusCode}.",
                    null);
            }

            var data = await response.ReadFromJsonAsync<TResponse>(cancellationToken);
            return OzonApiResult<TResponse>.Success(data!);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            _logger.LogWarning("Ozon API call to {Path} was cancelled.", path);
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error while calling Ozon API at {Path}.", path);

            return OzonApiResult<TResponse>.Failure(
                statusCode: null,
                message: ex.Message);
        }
    }

    /// <summary>
    /// Сериализует DateTime всегда как UTC (добавляет суффикс Z), чего требует Ozon API.
    /// </summary>
    private sealed class UtcDateTimeJsonConverter : System.Text.Json.Serialization.JsonConverter<DateTime>
    {
        public override DateTime Read(ref System.Text.Json.Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
            => reader.GetDateTime();

        public override void Write(System.Text.Json.Utf8JsonWriter writer, DateTime value, JsonSerializerOptions options)
            => writer.WriteStringValue(DateTime.SpecifyKind(value, DateTimeKind.Utc));
    }
}
