using StudioB2B.Infrastructure.Integrations.Ozon.Models.FbsUnfulfilled;
using StudioB2B.Infrastructure.Integrations.Ozon.Models.ProductAttributes;
using StudioB2B.Infrastructure.Integrations.Ozon.Models.ProductPrices;
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
}
