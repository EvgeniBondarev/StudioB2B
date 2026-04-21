using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using StudioB2B.Domain.Options;
using StudioB2B.Infrastructure.Services;
using StudioB2B.Infrastructure.Services.Ozon;
using WireMock.RequestBuilders;
using WireMock.ResponseBuilders;
using WireMock.Server;
using Xunit;

namespace StudioB2B.Tests.Integration.Ozon;

/// <summary>
/// Tests for OzonApiClient using WireMock.Net to simulate the Ozon API.
/// </summary>
public class OzonApiClientTests : IDisposable
{
    private readonly WireMockServer _server;
    private readonly OzonApiClient _client;
    private readonly KeyEncryptionService _encryption;

    public OzonApiClientTests()
    {
        _server = WireMockServer.Start();

        _encryption = new KeyEncryptionService(Options.Create(new EncryptionOptions()));

        var httpClientFactory = new TestHttpClientFactory(_server.Url!);
        _client = new OzonApiClient(httpClientFactory, _encryption, NullLogger<OzonApiClient>.Instance);
    }

    [Fact]
    public async Task GetSellerInfoAsync_ValidCredentials_ReturnsSuccess()
    {
        _server.Given(
            Request.Create().WithPath("/v3/product/info/stocks").UsingPost()
        ).RespondWith(
            Response.Create().WithStatusCode(200).WithBodyAsJson(new
            {
                result = new { name = "TestSeller", company_id = 123 }
            })
        );

        // Seller info endpoint
        _server.Given(
            Request.Create().WithPath("/v1/seller/info").UsingPost()
        ).RespondWith(
            Response.Create().WithStatusCode(200).WithBodyAsJson(new
            {
                result = new { name = "TestSeller", company_id = 123 }
            })
        );

        var encryptedKey = _encryption.Encrypt("valid-api-key");
        var result = await _client.GetSellerInfoAsync("123", encryptedKey);

        result.Should().NotBeNull();
        result.IsSuccess.Should().BeTrue();
    }

    [Fact]
    public async Task GetSellerInfoAsync_EmptyClientId_ThrowsArgumentException()
    {
        var act = async () => await _client.GetSellerInfoAsync("", "any-key");
        await act.Should().ThrowAsync<ArgumentException>()
            .WithParameterName("clientId");
    }

    [Fact]
    public async Task GetSellerInfoAsync_EmptyApiKey_ThrowsArgumentException()
    {
        var act = async () => await _client.GetSellerInfoAsync("123", "");
        await act.Should().ThrowAsync<ArgumentException>()
            .WithParameterName("apiKey");
    }

    [Fact]
    public async Task GetSellerInfoAsync_ServerReturns401_ReturnsErrorResult()
    {
        _server.Given(
            Request.Create().WithPath("/v1/seller/info").UsingPost()
        ).RespondWith(
            Response.Create().WithStatusCode(401).WithBody("{\"code\":16,\"message\":\"Client-Id and Api-Key headers are required\"}")
        );

        var encryptedKey = _encryption.Encrypt("bad-key");
        var result = await _client.GetSellerInfoAsync("000", encryptedKey);

        result.IsSuccess.Should().BeFalse();
        result.ErrorMessage.Should().NotBeNullOrEmpty();
    }

    public void Dispose()
    {
        _server.Dispose();
        GC.SuppressFinalize(this);
    }

    private sealed class TestHttpClientFactory : IHttpClientFactory
    {
        private readonly string _baseUrl;
        public TestHttpClientFactory(string baseUrl) => _baseUrl = baseUrl;

        public HttpClient CreateClient(string name) => new()
        {
            BaseAddress = new Uri(_baseUrl),
            Timeout = TimeSpan.FromSeconds(10)
        };
    }
}
