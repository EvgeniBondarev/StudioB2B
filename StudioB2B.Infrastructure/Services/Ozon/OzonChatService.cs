using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using StudioB2B.Domain.Constants;
using StudioB2B.Infrastructure.Interfaces;
using StudioB2B.Shared;

namespace StudioB2B.Infrastructure.Services.Ozon;

public class OzonChatService : IOzonChatService
{
    private readonly ITenantDbContextFactory _dbFactory;
    private readonly IOzonApiClient _ozonApi;
    private readonly ILogger<OzonChatService> _logger;
    private readonly IEntityFilterService _entityFilter;

    public OzonChatService(ITenantDbContextFactory dbFactory, IOzonApiClient ozonApi,
        ILogger<OzonChatService> logger, IEntityFilterService entityFilter)
    {
        _dbFactory = dbFactory;
        _ozonApi = ozonApi;
        _logger = logger;
        _entityFilter = entityFilter;
    }

    public async Task<OzonChatPageDto> GetChatsPageAsync(int pageSize = 20, string? cursor = null, string? chatStatus = null,
                                                         string? chatType = null, bool unreadOnly = false,
                                                         Guid? marketplaceClientId = null, CancellationToken ct = default)
    {
        var clients = await GetOzonClientsAsync(marketplaceClientId, ct);
        var viewModels = new List<OzonChatViewModelDto>();
        string? nextCursor = null;

        // Курсор кодируем как "clientIndex:ozonCursor"
        var startClientIndex = 0;
        string? ozonCursor = null;
        if (!string.IsNullOrEmpty(cursor))
        {
            var sep = cursor.IndexOf(':');
            if (sep > 0)
            {
                if (!int.TryParse(cursor[..sep], out startClientIndex))
                    startClientIndex = 0;
                ozonCursor = cursor[(sep + 1)..];
                if (ozonCursor == "") ozonCursor = null;
            }
        }

        var remaining = pageSize;

        for (var ci = startClientIndex; ci < clients.Count && remaining > 0; ci++)
        {
            var client = clients[ci];
            try
            {
                var request = new OzonChatListRequestDto
                {
                    Limit = remaining,
                    Cursor = ci == startClientIndex ? ozonCursor : null,
                    Filter = new OzonChatListFilterDto
                    {
                        ChatStatus = string.IsNullOrEmpty(chatStatus) ? null : chatStatus,
                        UnreadOnly = unreadOnly ? true : null
                    }
                };

                var apiResult = await _ozonApi.GetChatListAsync(client.ApiId, client.EncryptedApiKey, request, ct);
                if (!apiResult.IsSuccess || apiResult.Data is null)
                {
                    _logger.LogWarning("GetChatsPage failed for client {Name}: {Error}", client.Name, apiResult.ErrorMessage);
                    continue;
                }

                var items = apiResult.Data.Chats
                    .Where(i => i.Chat is not null)
                    .Where(i => string.IsNullOrEmpty(chatType) ||
                                string.Equals(i.Chat!.ChatType, chatType, StringComparison.OrdinalIgnoreCase))
                    .ToList();

                // Параллельно получаем время последнего сообщения для каждого чата
                var historyTasks = items.Select(item => GetLastMessageTimeAsync(
                    client.ApiId, client.EncryptedApiKey, item.Chat!.ChatId, item.Chat.CreatedAt, ct));
                var lastTimes = await Task.WhenAll(historyTasks);

                for (int i = 0; i < items.Count; i++)
                {
                    var item = items[i];
                    viewModels.Add(new OzonChatViewModelDto
                    {
                        MarketplaceClientId = client.Id,
                        MarketplaceClientName = client.Name,
                        ApiId = client.ApiId,
                        ApiKey = client.EncryptedApiKey,
                        ChatId = item.Chat!.ChatId,
                        ChatStatus = item.Chat.ChatStatus,
                        ChatType = item.Chat.ChatType,
                        CreatedAt = item.Chat.CreatedAt,
                        LastMessageAt = lastTimes[i],
                        FirstUnreadMessageId = item.FirstUnreadMessageId,
                        LastMessageId = item.LastMessageId,
                        UnreadCount = item.UnreadCount
                    });
                }

                remaining -= items.Count;

                // Если у этого клиента есть ещё страницы
                if (apiResult.Data.HasNext && remaining <= 0)
                {
                    nextCursor = $"{ci}:{apiResult.Data.Cursor}";
                }
                else if (apiResult.Data.HasNext && remaining > 0)
                {
                    // Продолжаем со следующего клиента но сохраняем курсор этого
                    nextCursor = $"{ci}:{apiResult.Data.Cursor}";
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching chats page for client {Name}", client.Name);
            }

            // Если перешли к следующему клиенту и ещё есть результаты
            if (remaining > 0 && ci + 1 < clients.Count)
                nextCursor = $"{ci + 1}:";
        }

        return new OzonChatPageDto
        {
            Chats = viewModels.OrderByDescending(c => c.LastMessageAt).ToList(),
            NextCursor = nextCursor
        };
    }

    private async Task<DateTime> GetLastMessageTimeAsync(string apiId, string encryptedApiKey, string chatId, DateTime fallback,
                                                         CancellationToken ct)
    {
        try
        {
            var req = new OzonChatHistoryRequestDto
            {
                ChatId = chatId,
                Direction = "Backward",
                Limit = 1
            };
            var result = await _ozonApi.GetChatHistoryAsync(apiId, encryptedApiKey, req, ct);
            if (result.IsSuccess && result.Data?.Messages.Count > 0)
                return result.Data.Messages[0].CreatedAt;
        }
        catch { /* fallback */ }
        return fallback;
    }

    public async Task<List<OzonChatViewModelDto>> GetAllChatsAsync(string? chatStatus = null, string? chatType = null,
                                                                   bool unreadOnly = false, Guid? marketplaceClientId = null,
                                                                   CancellationToken ct = default)
    {
        var clients = await GetOzonClientsAsync(marketplaceClientId, ct);
        var result = new List<OzonChatViewModelDto>();

        foreach (var client in clients)
        {
            try
            {
                var request = new OzonChatListRequestDto
                {
                    Limit = 100,
                    Filter = new OzonChatListFilterDto
                    {
                        ChatStatus = string.IsNullOrEmpty(chatStatus) ? null : chatStatus,
                        UnreadOnly = unreadOnly ? true : null
                    }
                };

                // Paginate through all chats using cursor
                var hasNext = true;
                while (hasNext)
                {
                    var apiResult = await _ozonApi.GetChatListAsync(client.ApiId, client.EncryptedApiKey, request, ct);
                    if (!apiResult.IsSuccess || apiResult.Data is null)
                    {
                        _logger.LogWarning("GetChatList failed for client {ClientName}: {Error}",
                            client.Name, apiResult.ErrorMessage);
                        break;
                    }

                    foreach (var item in apiResult.Data.Chats)
                    {
                        if (item.Chat is null) continue;

                        // Apply chat type filter (API doesn't support it, so filter client-side)
                        if (!string.IsNullOrEmpty(chatType) &&
                            !string.Equals(item.Chat.ChatType, chatType, StringComparison.OrdinalIgnoreCase))
                            continue;

                        result.Add(new OzonChatViewModelDto
                        {
                            MarketplaceClientId = client.Id,
                            MarketplaceClientName = client.Name,
                            ApiId = client.ApiId,
                            ApiKey = client.EncryptedApiKey,
                            ChatId = item.Chat.ChatId,
                            ChatStatus = item.Chat.ChatStatus,
                            ChatType = item.Chat.ChatType,
                            CreatedAt = item.Chat.CreatedAt,
                            LastMessageAt = item.Chat.CreatedAt,
                            FirstUnreadMessageId = item.FirstUnreadMessageId,
                            LastMessageId = item.LastMessageId,
                            UnreadCount = item.UnreadCount
                        });
                    }

                    hasNext = apiResult.Data.HasNext;
                    if (hasNext)
                        request.Cursor = apiResult.Data.Cursor;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching chats for client {ClientName}", client.Name);
            }
        }

        return result.OrderByDescending(c => c.UnreadCount).ThenByDescending(c => c.CreatedAt).ToList();
    }

    public async Task<OzonChatHistoryResponseDto?> GetChatHistoryAsync(Guid marketplaceClientId, string chatId, string direction = "Backward",
                                                                    ulong? fromMessageId = null, int limit = 50, CancellationToken ct = default)
    {
        var client = await GetClientOrNullAsync(marketplaceClientId, ct);
        if (client is null) return null;

        var request = new OzonChatHistoryRequestDto
        {
            ChatId = chatId,
            Direction = direction,
            FromMessageId = fromMessageId,
            Limit = limit
        };

        var result = await _ozonApi.GetChatHistoryAsync(client.ApiId, client.EncryptedApiKey, request, ct);
        if (!result.IsSuccess)
        {
            _logger.LogWarning("GetChatHistory failed for chat {ChatId}: {Error}", chatId, result.ErrorMessage);
            return null;
        }

        return result.Data;
    }

    public async Task<(bool Ok, string? Error)> SendMessageAsync(Guid marketplaceClientId, string chatId, string text,
                                                                 CancellationToken ct = default)
    {
        var client = await GetClientOrNullAsync(marketplaceClientId, ct);
        if (client is null) return (false, "Маркетплейс-клиент не найден");

        var result = await _ozonApi.SendChatMessageAsync(client.ApiId, client.EncryptedApiKey, chatId, text, ct);
        if (!result.IsSuccess)
        {
            _logger.LogWarning("SendMessage failed for chat {ChatId}: {Error}", chatId, result.ErrorMessage);
            return (false, GetFriendlyError(result.StatusCode, result.ErrorMessage));
        }

        return (result.Data?.Result == "success", null);
    }

    public async Task<(bool Ok, string? Error)> SendFileAsync(Guid marketplaceClientId, string chatId, string base64Content,
                                                              string fileName, CancellationToken ct = default)
    {
        var client = await GetClientOrNullAsync(marketplaceClientId, ct);
        if (client is null) return (false, "Маркетплейс-клиент не найден");

        var result = await _ozonApi.SendChatFileAsync(client.ApiId, client.EncryptedApiKey, chatId, base64Content, fileName, ct);
        if (!result.IsSuccess)
        {
            _logger.LogWarning("SendFile failed for chat {ChatId}: {Error}", chatId, result.ErrorMessage);
            return (false, GetFriendlyError(result.StatusCode, result.ErrorMessage));
        }

        return (result.Data?.Result == "success", null);
    }

    private static string GetFriendlyError(int? statusCode, string? message)
    {
        if (statusCode == 403)
        {
            if (message != null && message.Contains("only replies are allowed"))
                return "В этом чате разрешены только ответы на сообщения поддержки.";
            return "Нет прав для отправки сообщения в этот чат.";
        }
        if (statusCode == 429)
            return "Слишком много запросов. Попробуйте позже.";
        return message ?? "Неизвестная ошибка";
    }

    public async Task<int> MarkReadAsync(Guid marketplaceClientId, string chatId, ulong? fromMessageId = null, CancellationToken ct = default)
    {
        var client = await GetClientOrNullAsync(marketplaceClientId, ct);
        if (client is null) return 0;

        var result = await _ozonApi.ReadChatAsync(client.ApiId, client.EncryptedApiKey, chatId, fromMessageId, ct);
        if (!result.IsSuccess)
            _logger.LogWarning("ReadChat failed for chat {ChatId}: {Error}", chatId, result.ErrorMessage);

        return result.Data?.UnreadCount ?? 0;
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
            // Explicit filter: validate it's in the allowed set
            if (allowedIds is not null && !allowedIds.Contains(filterById.Value))
                return [];
            query = query.Where(c => c.Id == filterById.Value);
        }
        else if (allowedIds is not null)
        {
            // Apply permission whitelist
            query = query.Where(c => allowedIds.Contains(c.Id));
        }

        var clients = await query
            .Select(c => new OzonChatClientInfoDto { Id = c.Id, Name = c.Name, ApiId = c.ApiId, EncryptedApiKey = c.Key })
            .ToListAsync(ct);

        return clients;
    }

    private async Task<OzonChatClientInfoDto?> GetClientOrNullAsync(Guid clientId, CancellationToken ct)
    {
        await using var db = _dbFactory.CreateDbContext();

        return await db.MarketplaceClients!
            .AsNoTracking()
            .Where(c => c.Id == clientId && !c.IsDeleted)
            .Select(c => new OzonChatClientInfoDto { Id = c.Id, Name = c.Name, ApiId = c.ApiId, EncryptedApiKey = c.Key })
            .FirstOrDefaultAsync(ct);
    }
}

