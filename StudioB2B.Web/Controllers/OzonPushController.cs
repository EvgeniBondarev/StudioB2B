using System.Net;
using System.Text.Json;
using System.Text.Json.Serialization;
using Hangfire;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using StudioB2B.Domain.Constants;
using StudioB2B.Infrastructure.Interfaces;
using StudioB2B.Infrastructure.Services.Communication;
using StudioB2B.Infrastructure.Services.MultiTenancy;
using StudioB2B.Shared;

namespace StudioB2B.Web.Controllers;

/// <summary>
/// Endpoint для получения push-уведомлений от Ozon.
/// URL регистрируется в личном кабинете Ozon на каждый тенант: https://{tenant}.studiob2b.ru/api/ozon/push
/// </summary>
[ApiController]
[Route("api/ozon/push")]
[AllowAnonymous]
public class OzonPushController : ControllerBase
{
    // Ozon IP-диапазоны: 195.34.21.0/24, 185.73.192.0/22, 91.223.93.0/24
    private static readonly (uint Network, uint Mask)[] OzonIpRanges =
    [
        (IpToUint("195.34.21.0"), IpToUint("255.255.255.0")),
        (IpToUint("185.73.192.0"), IpToUint("255.255.252.0")),
        (IpToUint("91.223.93.0"), IpToUint("255.255.255.0"))
    ];

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    };

    private readonly IOzonPushNotificationService _pushService;
    private readonly IOzonPushNotificationSender _pushSender;
    private readonly ITenantProvider _tenantProvider;
    private readonly TenantHangfireManager _hangfireManager;
    private readonly ILogger<OzonPushController> _logger;

    public OzonPushController(
        IOzonPushNotificationService pushService,
        IOzonPushNotificationSender pushSender,
        ITenantProvider tenantProvider,
        TenantHangfireManager hangfireManager,
        ILogger<OzonPushController> logger)
    {
        _pushService = pushService;
        _pushSender = pushSender;
        _tenantProvider = tenantProvider;
        _hangfireManager = hangfireManager;
        _logger = logger;
    }

    [HttpPost]
    public async Task<IActionResult> Receive(CancellationToken ct)
    {
        // if (!IsOzonIp())
        // {
        //     _logger.LogWarning("OzonPush: request from unknown IP {IP} rejected.", GetRemoteIp());
        //     return StatusCode(403, OzonError("ERROR_UNKNOWN", "Forbidden."));
        // }

        if (!_tenantProvider.IsResolved)
            return StatusCode(503, OzonError("ERROR_UNKNOWN", "Tenant not resolved."));

        string body;
        using (var reader = new StreamReader(Request.Body))
            body = await reader.ReadToEndAsync(ct);

        if (string.IsNullOrWhiteSpace(body))
            return BadRequest(OzonError("ERROR_PARAMETER_VALUE_MISSED", "Empty body."));

        JsonDocument doc;
        try
        {
            doc = JsonDocument.Parse(body);
        }
        catch (JsonException ex)
        {
            _logger.LogWarning(ex, "OzonPush: invalid JSON.");
            return BadRequest(OzonError("ERROR_UNKNOWN", "Invalid JSON."));
        }

        if (!doc.RootElement.TryGetProperty("message_type", out var typeProp))
            return BadRequest(OzonError("ERROR_PARAMETER_VALUE_MISSED", "message_type is missing."));

        var messageType = typeProp.GetString() ?? string.Empty;

        // TYPE_PING — connection check, respond without saving
        if (messageType == OzonPushMessageType.Ping)
        {
            return Ok(new
            {
                version = "1.0",
                name = "StudioB2B",
                time = DateTime.UtcNow
            });
        }

        // Extract seller_id and posting_number for all other types
        long? sellerId = null;
        if (doc.RootElement.TryGetProperty("seller_id", out var sellerProp))
        {
            if (sellerProp.ValueKind == JsonValueKind.Number)
                sellerId = sellerProp.GetInt64();
            else if (sellerProp.ValueKind == JsonValueKind.String &&
                     long.TryParse(sellerProp.GetString(), out var parsed))
                sellerId = parsed;
        }

        string? postingNumber = null;
        if (doc.RootElement.TryGetProperty("posting_number", out var postingProp))
            postingNumber = postingProp.GetString();

        // Extract chat_id for chat-related notifications
        string? chatId = null;
        if (doc.RootElement.TryGetProperty("chat_id", out var chatProp))
            chatId = chatProp.GetString();

        // Extract message text (data[0]) for TYPE_NEW_MESSAGE / TYPE_UPDATE_MESSAGE
        string? messageText = null;
        if ((messageType == OzonPushMessageType.NewMessage || messageType == OzonPushMessageType.UpdateMessage) &&
            doc.RootElement.TryGetProperty("data", out var dataProp) &&
            dataProp.ValueKind == JsonValueKind.Array &&
            dataProp.GetArrayLength() > 0)
        {
            messageText = dataProp[0].GetString();
        }

        try
        {
            var dto = await _pushService.SaveAsync(messageType, body, sellerId, postingNumber, chatId, messageText, ct);

            if (_tenantProvider.TenantId.HasValue)
                await _pushSender.SendPushAsync(_tenantProvider.TenantId.Value.ToString(), dto, ct);

            // Enqueue targeted chat upsert for chat-type notifications
            if (OzonPushMessageType.ChatTypes.Contains(messageType) &&
                chatId is not null &&
                _tenantProvider.TenantId.HasValue &&
                _tenantProvider.ConnectionString is not null)
            {
                try
                {
                    var jobClient = _hangfireManager.GetClient(_tenantProvider.TenantId.Value);
                    jobClient.Enqueue<CommunicationTaskSyncJob>(j => j.UpsertChatTaskAsync(
                        _tenantProvider.TenantId.Value,
                        _tenantProvider.ConnectionString,
                        chatId,
                        messageType,
                        CancellationToken.None));
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "OzonPush: failed to enqueue chat upsert for chat {ChatId}", chatId);
                }
            }

            _logger.LogInformation(
                "OzonPush: saved {MessageType} for seller {SellerId}, tenant {TenantId}.",
                messageType, sellerId, _tenantProvider.TenantId);

            return Ok(new { result = true });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "OzonPush: error saving notification of type {MessageType}.", messageType);
            return StatusCode(500, OzonError("ERROR_UNKNOWN", ex.Message));
        }
    }

    private bool IsOzonIp()
    {
        var remoteIp = GetRemoteIp();
        if (remoteIp is null) return false;
        if (!remoteIp.IsIPv4MappedToIPv6 && remoteIp.AddressFamily != System.Net.Sockets.AddressFamily.InterNetwork)
            return false;

        var actual = remoteIp.IsIPv4MappedToIPv6
            ? remoteIp.MapToIPv4()
            : remoteIp;

        var bytes = actual.GetAddressBytes();
        var ip = (uint)(bytes[0] << 24 | bytes[1] << 16 | bytes[2] << 8 | bytes[3]);

        foreach (var (network, mask) in OzonIpRanges)
        {
            if ((ip & mask) == (network & mask))
                return true;
        }

        return false;
    }

    private IPAddress? GetRemoteIp()
        => HttpContext.Connection.RemoteIpAddress;

    private static uint IpToUint(string ip)
    {
        var parts = ip.Split('.');
        return (uint)(byte.Parse(parts[0]) << 24 |
                      byte.Parse(parts[1]) << 16 |
                      byte.Parse(parts[2]) << 8 |
                      byte.Parse(parts[3]));
    }

    private static object OzonError(string code, string message) =>
        new { error = new { code, message, details = (string?)null } };
}

