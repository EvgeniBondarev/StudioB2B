using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using StudioB2B.Infrastructure.Interfaces;
using StudioB2B.Shared.DTOs;

namespace StudioB2B.Infrastructure.Services.Ozon;

public class OzonReviewsService : IOzonReviewsService
{
    private readonly ITenantDbContextFactory _dbFactory;
    private readonly IOzonApiClient _ozonApi;
    private readonly ILogger<OzonReviewsService> _logger;

    public OzonReviewsService(ITenantDbContextFactory dbFactory, IOzonApiClient ozonApi,
        ILogger<OzonReviewsService> logger)
    {
        _dbFactory = dbFactory;
        _ozonApi = ozonApi;
        _logger = logger;
    }

    // ── GetReviewsPageAsync ───────────────────────────────────────────────────

    public async Task<OzonReviewPageDto> GetReviewsPageAsync(
        int pageSize = 20,
        string? cursor = null,
        string? status = null,
        Guid? marketplaceClientId = null,
        CancellationToken ct = default)
    {
        var clients = await GetOzonClientsAsync(marketplaceClientId, ct);
        var items = new List<OzonReviewViewModelDto>();
        string? nextCursor = null;

        // Cursor encoded as "clientIndex:lastId"
        var startClientIndex = 0;
        string? lastId = null;
        if (!string.IsNullOrEmpty(cursor))
        {
            var sep = cursor.IndexOf(':');
            if (sep >= 0)
            {
                if (!int.TryParse(cursor[..sep], out startClientIndex))
                    startClientIndex = 0;
                lastId = cursor[(sep + 1)..];
                if (lastId == "") lastId = null;
            }
        }

        var remaining = pageSize;
        var ozonStatus = string.IsNullOrWhiteSpace(status) ? "ALL" : status;

        for (var ci = startClientIndex; ci < clients.Count && remaining > 0; ci++)
        {
            var client = clients[ci];
            try
            {
                var request = new OzonReviewListRequestDto
                {
                    Limit = Math.Min(remaining + 1, 100), // fetch slightly more to detect has_next
                    LastId = ci == startClientIndex && lastId is not null ? lastId : string.Empty,
                    SortDir = "DESC",
                    Status = ozonStatus
                };

                var apiResult = await _ozonApi.GetReviewListAsync(client.ApiId, client.EncryptedApiKey, request, ct);
                if (!apiResult.IsSuccess || apiResult.Data is null)
                {
                    _logger.LogWarning("GetReviewList failed for client {Name}: {Error}", client.Name, apiResult.ErrorMessage);
                    continue;
                }

                foreach (var r in apiResult.Data.Reviews)
                {
                    if (remaining <= 0) break;

                    items.Add(new OzonReviewViewModelDto
                    {
                        MarketplaceClientId = client.Id,
                        MarketplaceClientName = client.Name,
                        ApiId = client.ApiId,
                        ApiKey = client.EncryptedApiKey,
                        Id = r.Id,
                        Sku = r.Sku,
                        Rating = r.Rating,
                        Text = r.Text,
                        Status = r.Status,
                        OrderStatus = r.OrderStatus,
                        CommentsAmount = r.CommentsAmount,
                        PhotosAmount = r.PhotosAmount,
                        VideosAmount = r.VideosAmount,
                        IsRatingParticipant = r.IsRatingParticipant,
                        PublishedAt = r.PublishedAt
                    });

                    remaining--;
                }

                if (apiResult.Data.HasNext && remaining <= 0)
                    nextCursor = $"{ci}:{apiResult.Data.LastId}";
                else if (apiResult.Data.HasNext && remaining > 0)
                    nextCursor = $"{ci}:{apiResult.Data.LastId}";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching reviews page for client {Name}", client.Name);
            }

            if (remaining > 0 && ci + 1 < clients.Count)
                nextCursor = $"{ci + 1}:";
        }

        return new OzonReviewPageDto
        {
            Reviews = items,
            NextCursor = nextCursor
        };
    }

    // ── GetReviewDetailAsync ──────────────────────────────────────────────────

    public async Task<OzonReviewDetailDto> GetReviewDetailAsync(
        OzonReviewViewModelDto review,
        CancellationToken ct = default)
    {
        var detail = new OzonReviewDetailDto { Review = review };

        // 1. Full info
        try
        {
            var infoResult = await _ozonApi.GetReviewInfoAsync(review.ApiId, review.ApiKey, review.Id, ct);
            if (infoResult.IsSuccess && infoResult.Data is not null)
            {
                var d = infoResult.Data;
                detail.Review = new OzonReviewViewModelDto
                {
                    MarketplaceClientId = review.MarketplaceClientId,
                    MarketplaceClientName = review.MarketplaceClientName,
                    ApiId = review.ApiId,
                    ApiKey = review.ApiKey,
                    Id = d.Id,
                    Sku = d.Sku,
                    Rating = d.Rating,
                    Text = d.Text,
                    Status = d.Status,
                    OrderStatus = d.OrderStatus,
                    CommentsAmount = d.CommentsAmount,
                    PhotosAmount = d.PhotosAmount,
                    VideosAmount = d.VideosAmount,
                    IsRatingParticipant = d.IsRatingParticipant,
                    PublishedAt = d.PublishedAt
                };
                detail.Photos = d.Photos;
                detail.Videos = d.Videos;
                detail.LikesAmount = d.LikesAmount;
                detail.DislikesAmount = d.DislikesAmount;
            }
            else
            {
                _logger.LogWarning("GetReviewInfo failed for review {Id}: {Error}", review.Id, infoResult.ErrorMessage);
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "GetReviewInfo exception for review {Id}", review.Id);
        }

        // 2. Comments
        try
        {
            var commentsResult = await _ozonApi.GetReviewCommentListAsync(
                review.ApiId, review.ApiKey,
                new OzonReviewCommentListRequestDto { ReviewId = review.Id, Limit = 100, SortDir = "ASC" },
                ct);

            if (commentsResult.IsSuccess && commentsResult.Data?.Comments.Count > 0)
                detail.Comments = commentsResult.Data.Comments;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "GetReviewCommentList failed for review {Id}", review.Id);
        }

        // 3. Product info via /v4/product/info/attributes (best-effort, same logic as Questions)
        var skuToLookup = detail.Review.Sku;
        try
        {
            await using var db = _dbFactory.CreateDbContext();
            var product = await db.Products!
                .AsNoTracking()
                .FirstOrDefaultAsync(p => p.Sku == skuToLookup && !p.IsDeleted, ct);

            OzonProductAttributesResponseDto? attrData = null;

            if (product?.Article is not null)
            {
                var attrResult = await _ozonApi.GetProductAttributesAsync(
                    review.ApiId, review.ApiKey, new[] { product.Article }, ct: ct);
                if (attrResult.IsSuccess)
                    attrData = attrResult.Data;
                else
                    _logger.LogWarning("Review product attributes by article failed: {Error}", attrResult.ErrorMessage);
            }
            else
            {
                var fallbackResult = await _ozonApi.GetProductAttributesBySkuAsync(
                    review.ApiId, review.ApiKey, new[] { skuToLookup }, ct);
                if (fallbackResult.IsSuccess && fallbackResult.Data?.Result.Count > 0)
                    attrData = fallbackResult.Data;
                else
                    _logger.LogWarning("Review product attributes SKU fallback returned no results for SKU {Sku}: {Error}",
                        skuToLookup, fallbackResult.ErrorMessage);
            }

            if (attrData?.Result.Count > 0)
            {
                var item = attrData.Result[0];

                var allImages = new List<string>();
                if (!string.IsNullOrWhiteSpace(item.PrimaryImage))
                    allImages.Add(item.PrimaryImage);
                if (item.Images is not null)
                    foreach (var img in item.Images)
                        if (!string.IsNullOrWhiteSpace(img) && !allImages.Contains(img))
                            allImages.Add(img);

                detail.Product = new OzonQuestionProductInfoDto
                {
                    Name = item.Name,
                    PrimaryImage = item.PrimaryImage,
                    Images = allImages,
                    Description = ExtractDescription(item.Attributes),
                    Sku = item.Sku,
                    OfferId = item.OfferId,
                    Barcode = item.Barcode,
                    Weight = item.Weight,
                    WeightUnit = item.WeightUnit,
                    Height = item.Height,
                    Width = item.Width,
                    Depth = item.Depth,
                    DimensionUnit = item.DimensionUnit
                };
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to fetch product info for review SKU {Sku}", skuToLookup);
        }

        return detail;
    }

    // ── ChangeReviewStatusAsync ───────────────────────────────────────────────

    public async Task<bool> ChangeReviewStatusAsync(
        OzonReviewViewModelDto review,
        string status,
        CancellationToken ct = default)
    {
        try
        {
            var result = await _ozonApi.ChangeReviewStatusAsync(
                review.ApiId, review.ApiKey, new[] { review.Id }, status, ct);

            if (result.IsSuccess) return true;

            _logger.LogWarning("ChangeReviewStatus failed for review {Id} status={Status}: {Error}",
                review.Id, status, result.ErrorMessage);
            return false;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "ChangeReviewStatus exception for review {Id}", review.Id);
            return false;
        }
    }

    // ── GetReviewCountsAsync ──────────────────────────────────────────────────

    public async Task<OzonReviewCountAggregateDto> GetReviewCountsAsync(
        Guid? marketplaceClientId = null,
        CancellationToken ct = default)
    {
        var clients = await GetOzonClientsAsync(marketplaceClientId, ct);
        var totals = new OzonReviewCountAggregateDto();

        foreach (var client in clients)
        {
            try
            {
                var result = await _ozonApi.GetReviewCountAsync(client.ApiId, client.EncryptedApiKey, ct);
                if (result.IsSuccess && result.Data is not null)
                {
                    totals.Processed += result.Data.Processed;
                    totals.Unprocessed += result.Data.Unprocessed;
                    totals.Total += result.Data.Total;
                }
                else
                {
                    _logger.LogWarning("GetReviewCount failed for client {Name}: {Error}",
                        client.Name, result.ErrorMessage);
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "GetReviewCount exception for client {Name}", client.Name);
            }
        }

        return totals;
    }

    // ── CreateReviewCommentAsync ──────────────────────────────────────────────

    public async Task<string?> CreateReviewCommentAsync(
        OzonReviewViewModelDto review,
        string text,
        bool markAsProcessed = true,
        CancellationToken ct = default)
    {
        try
        {
            var result = await _ozonApi.CreateReviewCommentAsync(
                review.ApiId, review.ApiKey,
                new OzonReviewCommentCreateRequestDto
                {
                    ReviewId = review.Id,
                    Text = text,
                    MarkReviewAsProcessed = markAsProcessed
                },
                ct);

            if (result.IsSuccess && !string.IsNullOrEmpty(result.Data?.CommentId))
                return result.Data.CommentId;

            _logger.LogWarning("CreateReviewComment failed for review {Id}: {Error}",
                review.Id, result.ErrorMessage);
            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "CreateReviewComment exception for review {Id}", review.Id);
            return null;
        }
    }

    // ── DeleteReviewCommentAsync ──────────────────────────────────────────────

    public async Task<bool> DeleteReviewCommentAsync(
        OzonReviewViewModelDto review,
        string commentId,
        CancellationToken ct = default)
    {
        try
        {
            var result = await _ozonApi.DeleteReviewCommentAsync(
                review.ApiId, review.ApiKey, commentId, ct);

            if (result.IsSuccess) return true;

            _logger.LogWarning("DeleteReviewComment failed for comment {CommentId}: {Error}",
                commentId, result.ErrorMessage);
            return false;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "DeleteReviewComment exception for comment {CommentId}", commentId);
            return false;
        }
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    // ── Helpers ───────────────────────────────────────────────────────────────

    /// <summary>
    /// Извлекает описание товара из атрибутов Ozon.
    /// Attribute_id 4191 — стандартное «Описание»; fallback — первый длинный текстовый атрибут.
    /// </summary>
    private static string? ExtractDescription(List<OzonAttributeDto> attributes)
    {
        var descAttr = attributes.FirstOrDefault(a => a.Id == 4191);
        if (descAttr?.Values.Count > 0 && !string.IsNullOrWhiteSpace(descAttr.Values[0].Value))
            return descAttr.Values[0].Value;

        return attributes
            .SelectMany(a => a.Values)
            .FirstOrDefault(v => v.Value?.Length > 50)?.Value;
    }

    private async Task<List<OzonChatClientInfoDto>> GetOzonClientsAsync(Guid? filterById, CancellationToken ct)
    {
        await using var db = _dbFactory.CreateDbContext();

        var query = db.MarketplaceClients!
            .AsNoTracking()
            .Where(c => !c.IsDeleted);

        if (filterById.HasValue)
            query = query.Where(c => c.Id == filterById.Value);

        return await query
            .Select(c => new OzonChatClientInfoDto
            {
                Id = c.Id,
                Name = c.Name,
                ApiId = c.ApiId,
                EncryptedApiKey = c.Key
            })
            .ToListAsync(ct);
    }
}

