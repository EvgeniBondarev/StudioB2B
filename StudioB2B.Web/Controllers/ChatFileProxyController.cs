using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using StudioB2B.Application.Common.Interfaces;
using StudioB2B.Infrastructure.Integrations.Ozon;
using StudioB2B.Infrastructure.Services;

namespace StudioB2B.Web.Controllers;

/// <summary>
/// Проксирует файлы/картинки из Ozon Chat API через бэкенд с авторизацией Client-Id/Api-Key.
/// Ozon требует заголовки авторизации даже для скачивания файлов чата.
/// </summary>
[ApiController]
[Route("api/chat")]
[Authorize]
public class ChatFileProxyController : ControllerBase
{
    private readonly IOzonApiClient _ozonApi;
    private readonly ITenantDbContextFactory _dbFactory;
    private readonly ITenantProvider _tenantProvider;

    public ChatFileProxyController(
        IOzonApiClient ozonApi,
        ITenantDbContextFactory dbFactory,
        ITenantProvider tenantProvider)
    {
        _ozonApi       = ozonApi;
        _dbFactory     = dbFactory;
        _tenantProvider = tenantProvider;
    }

    /// <summary>
    /// GET /api/chat/file?url=https://api-seller.ozon.ru/v2/chat/file/...&amp;clientId=...
    /// Скачивает файл через Ozon API с авторизацией и отдаёт браузеру.
    /// </summary>
    [HttpGet("file")]
    public async Task<IActionResult> ProxyFile(
        [FromQuery] string url,
        [FromQuery] Guid clientId,
        [FromQuery] string? name = null,
        CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(url))
            return BadRequest("url is required");

        if (!IsAllowedUrl(url))
            return BadRequest("URL не разрешён");

        if (!_tenantProvider.IsResolved)
            return BadRequest("Tenant not resolved");

        // Получаем credentials клиента из БД
        await using var db = _dbFactory.CreateDbContext();
        var client = await db.MarketplaceClients!
            .Where(c => c.Id == clientId && !c.IsDeleted)
            .Select(c => new { c.ApiId, c.Key })
            .FirstOrDefaultAsync(ct);

        if (client is null)
            return NotFound("Маркетплейс-клиент не найден");

        var (stream, contentType, success) = await _ozonApi.DownloadChatFileAsync(
            client.ApiId, client.Key, url, ct);

        if (!success || stream is null)
            return StatusCode(502, "Не удалось получить файл от Ozon");

        var fileName = string.IsNullOrWhiteSpace(name)
            ? Path.GetFileName(new Uri(url).AbsolutePath)
            : name;
        if (string.IsNullOrWhiteSpace(fileName)) fileName = "file";

        // Fallback contentType по расширению если Ozon вернул generic
        if (contentType is "application/octet-stream" or "")
            contentType = GetMimeByExtension(fileName);

        var isInline = contentType.StartsWith("image/", StringComparison.OrdinalIgnoreCase);

        // RFC 5987 для корректной передачи не-ASCII имён файлов
        var encodedName = Uri.EscapeDataString(fileName);
        Response.Headers["Content-Disposition"] = isInline
            ? $"inline; filename*=UTF-8''{encodedName}"
            : $"attachment; filename*=UTF-8''{encodedName}";

        return File(stream, contentType);
    }

    private static string GetMimeByExtension(string fileName)
    {
        var ext = Path.GetExtension(fileName).ToLowerInvariant();
        return ext switch
        {
            ".jpg" or ".jpeg" => "image/jpeg",
            ".png"            => "image/png",
            ".gif"            => "image/gif",
            ".webp"           => "image/webp",
            ".pdf"            => "application/pdf",
            ".mp4"            => "video/mp4",
            ".mov"            => "video/quicktime",
            ".avi"            => "video/x-msvideo",
            ".webm"           => "video/webm",
            ".mp3"            => "audio/mpeg",
            ".ogg"            => "audio/ogg",
            ".wav"            => "audio/wav",
            ".doc"            => "application/msword",
            ".docx"           => "application/vnd.openxmlformats-officedocument.wordprocessingml.document",
            ".txt"            => "text/plain",
            ".zip"            => "application/zip",
            _                 => "application/octet-stream"
        };
    }

    private static bool IsAllowedUrl(string url)
    {
        try
        {
            var uri = new Uri(url);
            return uri.Scheme is "https" or "http" &&
                   (uri.Host.EndsWith("ozon.ru", StringComparison.OrdinalIgnoreCase) ||
                    uri.Host.EndsWith("ozon.com", StringComparison.OrdinalIgnoreCase));
        }
        catch { return false; }
    }
}

