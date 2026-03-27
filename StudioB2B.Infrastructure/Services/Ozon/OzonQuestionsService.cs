using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using StudioB2B.Domain.Constants;
using StudioB2B.Infrastructure.Interfaces;
using StudioB2B.Shared;

namespace StudioB2B.Infrastructure.Services.Ozon;

public class OzonQuestionsService : IOzonQuestionsService
{
    private readonly ITenantDbContextFactory _dbFactory;
    private readonly IOzonApiClient _ozonApi;
    private readonly ILogger<OzonQuestionsService> _logger;
    private readonly IEntityFilterService _entityFilter;

    public OzonQuestionsService(ITenantDbContextFactory dbFactory, IOzonApiClient ozonApi,
        ILogger<OzonQuestionsService> logger, IEntityFilterService entityFilter)
    {
        _dbFactory = dbFactory;
        _ozonApi = ozonApi;
        _logger = logger;
        _entityFilter = entityFilter;
    }

    public async Task<OzonQuestionPageDto> GetQuestionsPageAsync(
        int pageSize = 20,
        string? cursor = null,
        DateTime? dateFrom = null,
        DateTime? dateTo = null,
        string? status = null,
        Guid? marketplaceClientId = null,
        CancellationToken ct = default)
    {
        var clients = await GetOzonClientsAsync(marketplaceClientId, ct);
        var items = new List<OzonQuestionViewModelDto>();
        string? nextCursor = null;

        // Курсор кодируем как "clientIndex:lastId"
        var startClientIndex = 0;
        string? lastId = null;
        if (!string.IsNullOrEmpty(cursor))
        {
            var sep = cursor.IndexOf(':');
            if (sep > 0)
            {
                if (!int.TryParse(cursor[..sep], out startClientIndex))
                    startClientIndex = 0;
                lastId = cursor[(sep + 1)..];
                if (lastId == "") lastId = null;
            }
        }

        var remaining = pageSize;

        for (var ci = startClientIndex; ci < clients.Count && remaining > 0; ci++)
        {
            var client = clients[ci];
            try
            {
                var filter = new OzonQuestionListFilterDto
                {
                    DateFrom = dateFrom,
                    DateTo = dateTo,
                    Status = ParseStatus(status)
                };

                var request = new OzonQuestionListRequestDto
                {
                    Filter = filter,
                    LastId = ci == startClientIndex ? lastId : null
                };

                var apiResult = await _ozonApi.GetQuestionListAsync(client.ApiId, client.EncryptedApiKey, request, ct);
                if (!apiResult.IsSuccess || apiResult.Data is null)
                {
                    _logger.LogWarning("GetQuestionsPage failed for client {Name}: {Error}", client.Name,
                        apiResult.ErrorMessage);
                    continue;
                }

                foreach (var q in apiResult.Data.Questions)
                {
                    if (remaining <= 0)
                        break;

                    items.Add(new OzonQuestionViewModelDto
                    {
                        MarketplaceClientId = client.Id,
                        MarketplaceClientName = client.Name,
                        ApiId = client.ApiId,
                        ApiKey = client.EncryptedApiKey,
                        Id = q.Id,
                        Sku = q.Sku,
                        ProductUrl = q.ProductUrl,
                        QuestionLink = q.QuestionLink,
                        AuthorName = q.AuthorName,
                        Text = q.Text,
                        Status = q.Status.ToString(),
                        AnswersCount = (int)q.AnswersCount,
                        PublishedAt = q.PublishedAt
                    });

                    remaining--;
                }

                // Если у этого клиента есть ещё вопросы (last_id непустой) и мы исчерпали лимит
                if (!string.IsNullOrEmpty(apiResult.Data.LastId) && remaining <= 0)
                {
                    nextCursor = $"{ci}:{apiResult.Data.LastId}";
                }
                else if (!string.IsNullOrEmpty(apiResult.Data.LastId) && remaining > 0)
                {
                    // продолжаем со следующего клиента, но помним last_id этого клиента
                    nextCursor = $"{ci}:{apiResult.Data.LastId}";
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching questions page for client {Name}", client.Name);
            }

            // Если перешли к следующему клиенту и ещё есть результаты
            if (remaining > 0 && ci + 1 < clients.Count)
                nextCursor = $"{ci + 1}:";
        }

        return new OzonQuestionPageDto
        {
            Questions = items
                .OrderByDescending(q => q.PublishedAt)
                .ToList(),
            NextCursor = nextCursor
        };
    }

    private static OzonQuestionStatusEnum? ParseStatus(string? status)
    {
        if (string.IsNullOrWhiteSpace(status))
            return null;

        if (Enum.TryParse<OzonQuestionStatusEnum>(status, ignoreCase: true, out var parsed))
            return parsed;

        return null;
    }

    public async Task<OzonQuestionDetailDto> GetQuestionDetailAsync(
        OzonQuestionViewModelDto question,
        CancellationToken ct = default)
    {
        var detail = new OzonQuestionDetailDto { Question = question };

        // 1. Try /v1/question/info (requires Premium Plus)
        try
        {
            var infoResult = await _ozonApi.GetQuestionInfoAsync(question.ApiId, question.ApiKey, question.Id, ct);
            if (infoResult.IsSuccess && infoResult.Data is not null)
            {
                var q = infoResult.Data;
                detail.Question = new OzonQuestionViewModelDto
                {
                    MarketplaceClientId = question.MarketplaceClientId,
                    MarketplaceClientName = question.MarketplaceClientName,
                    ApiId = question.ApiId,
                    ApiKey = question.ApiKey,
                    Id = q.Id,
                    Sku = q.Sku,
                    ProductUrl = q.ProductUrl,
                    QuestionLink = q.QuestionLink,
                    AuthorName = q.AuthorName,
                    Text = q.Text,
                    Status = q.Status.ToString(),
                    AnswersCount = (int)q.AnswersCount,
                    PublishedAt = q.PublishedAt
                };
                detail.QuestionInfoAvailable = true;
            }
            else
            {
                _logger.LogWarning("GetQuestionInfo failed for question {Id}: {Error}", question.Id, infoResult.ErrorMessage);
                detail.QuestionInfoAvailable = false;
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "GetQuestionInfo exception for question {Id}", question.Id);
            detail.QuestionInfoAvailable = false;
        }

        // 2. Fetch product info via /v4/product/info/attributes
        try
        {
            await using var db = _dbFactory.CreateDbContext();
            var product = await db.Products!
                .AsNoTracking()
                .FirstOrDefaultAsync(p => p.Sku == question.Sku && !p.IsDeleted, ct);

            OzonProductAttributesResponseDto? attrData = null;

            if (product?.Article is not null)
            {
                _logger.LogDebug("GetQuestionDetail: found product in DB by SKU {Sku}, article={Article}",
                    question.Sku, product.Article);
                var attrResult = await _ozonApi.GetProductAttributesAsync(
                    question.ApiId, question.ApiKey, new[] { product.Article }, ct: ct);
                if (attrResult.IsSuccess)
                    attrData = attrResult.Data;
                else
                    _logger.LogWarning("GetProductAttributes by article failed: {Error}", attrResult.ErrorMessage);
            }
            else
            {
                _logger.LogWarning(
                    "GetQuestionDetail: product not found in DB by SKU {Sku} (product={Found}, article={Article}). Falling back to product_id lookup.",
                    question.Sku,
                    product is not null ? "found" : "not found",
                    product?.Article ?? "null");

                // Fallback: call /v4/product/info/attributes with filter.sku = [sku]
                var fallbackResult = await _ozonApi.GetProductAttributesBySkuAsync(
                    question.ApiId, question.ApiKey, new[] { question.Sku }, ct);
                if (fallbackResult.IsSuccess && fallbackResult.Data?.Result.Count > 0)
                {
                    _logger.LogDebug("GetQuestionDetail: product_id fallback succeeded for SKU {Sku}", question.Sku);
                    attrData = fallbackResult.Data;
                }
                else
                {
                    _logger.LogWarning(
                        "GetQuestionDetail: product_id fallback also returned no results for SKU {Sku}: {Error}",
                        question.Sku, fallbackResult.ErrorMessage);
                }
            }

            if (attrData?.Result.Count > 0)
            {
                var item = attrData.Result[0];

                // Collect all images: primary + images array (deduplicated)
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
            _logger.LogWarning(ex, "Failed to fetch product info for SKU {Sku}", question.Sku);
        }

        // 3. Fetch answers via /v1/question/answer/list (Premium Plus, best-effort)
        try
        {
            var answersResult = await _ozonApi.GetQuestionAnswersAsync(
                question.ApiId, question.ApiKey, question.Id, question.Sku, ct);
            if (answersResult.IsSuccess && answersResult.Data?.Answers.Count > 0)
                detail.Answers = answersResult.Data.Answers;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "GetQuestionAnswers failed for question {Id}", question.Id);
        }

        return detail;
    }

    /// <summary>
    /// Extracts the product description from Ozon attribute values.
    /// Ozon stores description as attribute_id 4191 ("Описание") in most categories.
    /// Falls back to the first long text attribute value if 4191 is not found.
    /// </summary>
    private static string? ExtractDescription(List<OzonAttributeDto> attributes)
    {
        // Standard Ozon description attribute_id = 4191
        var descAttr = attributes.FirstOrDefault(a => a.Id == 4191);
        if (descAttr?.Values.Count > 0 && !string.IsNullOrWhiteSpace(descAttr.Values[0].Value))
            return descAttr.Values[0].Value;

        // Fallback: first attribute with a text value longer than 50 characters
        return attributes
            .SelectMany(a => a.Values)
            .FirstOrDefault(v => v.Value?.Length > 50)?.Value;
    }

    public async Task<bool> DeleteQuestionAnswerAsync(
        OzonQuestionViewModelDto question,
        string answerId,
        CancellationToken ct = default)
    {
        try
        {
            var result = await _ozonApi.DeleteQuestionAnswerAsync(
                question.ApiId, question.ApiKey, answerId, question.Sku, ct);

            if (result.IsSuccess)
                return true;

            _logger.LogWarning("DeleteQuestionAnswer failed for answer {AnswerId}: {Error}",
                answerId, result.ErrorMessage);
            return false;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "DeleteQuestionAnswer exception for answer {AnswerId}", answerId);
            return false;
        }
    }

    public async Task<string?> CreateQuestionAnswerAsync(
        OzonQuestionViewModelDto question,
        string text,
        CancellationToken ct = default)
    {
        try
        {
            var result = await _ozonApi.CreateQuestionAnswerAsync(
                question.ApiId, question.ApiKey, question.Id, question.Sku, text, ct);

            if (result.IsSuccess && !string.IsNullOrEmpty(result.Data?.AnswerId))
                return result.Data.AnswerId;

            _logger.LogWarning("CreateQuestionAnswer failed for question {Id}: {Error}",
                question.Id, result.ErrorMessage);
            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "CreateQuestionAnswer exception for question {Id}", question.Id);
            return null;
        }
    }

    public async Task<bool> ChangeQuestionStatusAsync(
        OzonQuestionViewModelDto question,
        string status,
        CancellationToken ct = default)
    {
        try
        {
            var result = await _ozonApi.ChangeQuestionStatusAsync(
                question.ApiId, question.ApiKey, new[] { question.Id }, status, ct);

            if (result.IsSuccess)
                return true;

            _logger.LogWarning("ChangeQuestionStatus failed for question {Id} status={Status}: {Error}",
                question.Id, status, result.ErrorMessage);
            return false;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "ChangeQuestionStatus exception for question {Id}", question.Id);
            return false;
        }
    }

    public async Task<OzonQuestionCountResponseDto> GetQuestionCountsAsync(
        Guid? marketplaceClientId = null,
        CancellationToken ct = default)
    {
        var clients = await GetOzonClientsAsync(marketplaceClientId, ct);
        var totals = new OzonQuestionCountResponseDto();

        foreach (var client in clients)
        {
            try
            {
                var result = await _ozonApi.GetQuestionCountAsync(client.ApiId, client.EncryptedApiKey, ct);
                if (result.IsSuccess && result.Data is not null)
                {
                    totals.All += result.Data.All;
                    totals.New += result.Data.New;
                    totals.Processed += result.Data.Processed;
                    totals.Unprocessed += result.Data.Unprocessed;
                    totals.Viewed += result.Data.Viewed;
                }
                else
                {
                    _logger.LogWarning("GetQuestionCount failed for client {Name}: {Error}",
                        client.Name, result.ErrorMessage);
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "GetQuestionCount exception for client {Name}", client.Name);
            }
        }

        return totals;
    }

    public async Task<List<OzonQuestionProductInfoDto>?> GetTopSkuProductsAsync(
        Guid? marketplaceClientId = null,
        int limit = 20,
        CancellationToken ct = default)
    {
        var clients = await GetOzonClientsAsync(marketplaceClientId, ct);
        var allSkus = new List<long>();

        foreach (var client in clients)
        {
            try
            {
                var result = await _ozonApi.GetQuestionTopSkuAsync(client.ApiId, client.EncryptedApiKey, limit, ct);
                if (result.IsSuccess && result.Data?.Sku.Count > 0)
                {
                    foreach (var sku in result.Data.Sku)
                        if (!allSkus.Contains(sku))
                            allSkus.Add(sku);
                }
                else if (result.ErrorMessage?.Contains("Premium Plus", StringComparison.OrdinalIgnoreCase) == true
                         || result.ErrorMessage?.Contains("PermissionDenied", StringComparison.OrdinalIgnoreCase) == true)
                {
                    _logger.LogWarning("GetQuestionTopSku: Premium Plus required for client {Name}", client.Name);
                    return null;
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "GetQuestionTopSku exception for client {Name}", client.Name);
            }
        }

        if (allSkus.Count == 0)
            return new List<OzonQuestionProductInfoDto>();

        var products = new List<OzonQuestionProductInfoDto>();
        var apiClient = clients.First();
        try
        {
            var attrResult = await _ozonApi.GetProductAttributesBySkuAsync(
                apiClient.ApiId, apiClient.EncryptedApiKey, allSkus, ct);

            if (attrResult.IsSuccess && attrResult.Data?.Result.Count > 0)
            {
                foreach (var item in attrResult.Data.Result)
                {
                    var allImages = new List<string>();
                    if (!string.IsNullOrWhiteSpace(item.PrimaryImage))
                        allImages.Add(item.PrimaryImage);
                    if (item.Images is not null)
                        foreach (var img in item.Images)
                            if (!string.IsNullOrWhiteSpace(img) && !allImages.Contains(img))
                                allImages.Add(img);

                    products.Add(new OzonQuestionProductInfoDto
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
                    });
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to enrich top SKUs with product info");
        }

        foreach (var sku in allSkus)
        {
            if (products.All(p => p.Sku != sku))
                products.Add(new OzonQuestionProductInfoDto { Sku = sku, Name = $"SKU {sku}" });
        }

        return allSkus
            .Select(sku => products.FirstOrDefault(p => p.Sku == sku))
            .Where(p => p is not null)
            .ToList()!;
    }

    private async Task<List<OzonChatClientInfoDto>> GetOzonClientsAsync(Guid? filterById, CancellationToken ct)
    {
        await using var db = _dbFactory.CreateDbContext();

        var allowedIds = await _entityFilter.GetAllowedIdsAsync(BlockedEntityTypeEnum.MarketplaceClient, ct);

        var query = db.MarketplaceClients!
            .AsNoTracking()
            .Where(c => !c.IsDeleted);

        if (filterById.HasValue)
        {
            if (allowedIds is not null && !allowedIds.Contains(filterById.Value))
                return [];
            query = query.Where(c => c.Id == filterById.Value);
        }
        else if (allowedIds is not null)
        {
            query = query.Where(c => allowedIds.Contains(c.Id));
        }

        var clients = await query
            .Select(c => new OzonChatClientInfoDto
            {
                Id = c.Id,
                Name = c.Name,
                ApiId = c.ApiId,
                EncryptedApiKey = c.Key
            })
            .ToListAsync(ct);

        return clients;
    }
}

