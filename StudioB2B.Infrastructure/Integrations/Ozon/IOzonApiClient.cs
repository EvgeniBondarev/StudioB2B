using StudioB2B.Infrastructure.Integrations.Ozon.Models.SellerInfo;

namespace StudioB2B.Infrastructure.Integrations.Ozon;

public interface IOzonApiClient
{
    /// <summary>
    /// Gets seller cabinet info (company, ratings, subscription) from Ozon Seller API.
    /// </summary>
    /// <param name="clientId">Ozon Client-Id (e.g. from MarketplaceClient.ApiId).</param>
    /// <param name="apiKey">Ozon Api-Key (e.g. from MarketplaceClient.Key).</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Result object with success flag, data or error details.</returns>
    Task<OzonApiResult<OzonSellerInfoResponse>> GetSellerInfoAsync(
        string clientId,
        string apiKey,
        CancellationToken cancellationToken = default);
}
