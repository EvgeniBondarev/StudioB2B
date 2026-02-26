using System.Net;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using StudioB2B.Application.Common.Interfaces;
using StudioB2B.Infrastructure.Http;
using StudioB2B.Infrastructure.Integrations.Ozon.Models.SellerInfo;

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

        // The stored key may be encrypted; decrypt it before calling the API
        var plainApiKey = _encryption.Decrypt(apiKey);

        return SendPostAsync<OzonSellerInfoResponse>(
            OzonEndpoints.SellerInfo,
            clientId,
            plainApiKey,
            new { },
            cancellationToken);
    }

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

        var json = JsonSerializer.Serialize(body);
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
}
