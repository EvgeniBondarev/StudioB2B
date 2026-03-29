using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using StudioB2B.Infrastructure.Interfaces;

namespace StudioB2B.Web.Controllers;

/// <summary>
/// Проксирует файлы/картинки из Ozon Chat API через бэкенд с авторизацией Client-Id/Api-Key.
/// Ozon требует заголовки авторизации даже для скачивания файлов чата.
/// </summary>
[ApiController]
[Route("api/chat")]
public class ChatFileProxyController : ControllerBase
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly IKeyEncryptionService _encryption;
    private readonly ITenantDbContextFactory _dbFactory;
    private readonly ITenantProvider _tenantProvider;

    public ChatFileProxyController(
        IHttpClientFactory httpClientFactory,
        IKeyEncryptionService encryption,
        ITenantDbContextFactory dbFactory,
        ITenantProvider tenantProvider)
    {
        _httpClientFactory = httpClientFactory;
        _encryption = encryption;
        _dbFactory = dbFactory;
        _tenantProvider = tenantProvider;
    }

    /// <summary>
    /// GET /api/chat/file?url=https://api-seller.ozon.ru/v2/chat/file/...&amp;clientId=...
    /// Скачивает файл через Ozon API с авторизацией и отдаёт браузеру.
    /// [AllowAnonymous]: браузер не отправляет JWT-токен автоматически для &lt;img src="..."&gt;,
    /// поэтому [Authorize] здесь не применим. Безопасность обеспечивается:
    /// - tenant резолвится из субдомена (TenantMiddleware);
    /// - clientId должен существовать в БД этого тенанта.
    /// </summary>
    [HttpGet("file")]
    [AllowAnonymous]
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

        await using var db = _dbFactory.CreateDbContext();
        var client = await db.MarketplaceClients!
            .Where(c => c.Id == clientId && !c.IsDeleted)
            .Select(c => new { c.ApiId, c.Key })
            .FirstOrDefaultAsync(ct);

        if (client is null)
            return NotFound("Маркетплейс-клиент не найден");

        var plainApiKey = _encryption.Decrypt(client.Key);

        var (stream, contentType) = await DownloadWithAuthAsync(
            url, client.ApiId, plainApiKey, ct);

        if (stream is null)
            return StatusCode(502, "Не удалось получить файл от Ozon");

        var fileName = string.IsNullOrWhiteSpace(name)
            ? Path.GetFileName(new Uri(url).AbsolutePath)
            : name;
        if (string.IsNullOrWhiteSpace(fileName)) fileName = "file";

        if (contentType is "application/octet-stream" or "")
            contentType = GetMimeByExtension(fileName);

        var isInline = contentType.StartsWith("image/", StringComparison.OrdinalIgnoreCase);
        var encodedName = Uri.EscapeDataString(fileName);
        Response.Headers.ContentDisposition = isInline
            ? $"inline; filename*=UTF-8''{encodedName}"
            : $"attachment; filename*=UTF-8''{encodedName}";

        return File(stream, contentType);
    }

    /// <summary>
    /// Скачивает файл напрямую, прокидывая Client-Id и Api-Key на каждый шаг,
    /// включая повторный запрос при редиректах (HttpClient по умолчанию теряет
    /// кастомные заголовки при автоматическом следовании редиректам).
    /// </summary>
    private async Task<(Stream? Content, string ContentType)> DownloadWithAuthAsync(
        string fileUrl, string ozonClientId, string plainApiKey,
        CancellationToken ct)
    {
        // Используем клиент без BaseAddress и без DelegatingHandler-ов,
        // чтобы они не мешали скачиванию файлов.
        // AllowAutoRedirect = false — следуем редиректам вручную, сохраняя заголовки.
        var http = _httpClientFactory.CreateClient("OzonFileProxy");

        const int maxRedirects = 5;
        var currentUrl = fileUrl;

        for (var i = 0; i < maxRedirects; i++)
        {
            using var request = new HttpRequestMessage(HttpMethod.Get, currentUrl);
            request.Headers.Add("Client-Id", ozonClientId);
            request.Headers.Add("Api-Key", plainApiKey);

            var response = await http.SendAsync(
                request, HttpCompletionOption.ResponseHeadersRead, ct);

            // Редирект — повторяем с тем же заголовком авторизации
            if (response.StatusCode is System.Net.HttpStatusCode.MovedPermanently
                                    or System.Net.HttpStatusCode.Found
                                    or System.Net.HttpStatusCode.TemporaryRedirect
                                    or System.Net.HttpStatusCode.PermanentRedirect)
            {
                var location = response.Headers.Location;
                if (location is null) break;
                currentUrl = location.IsAbsoluteUri
                    ? location.AbsoluteUri
                    : new Uri(new Uri(currentUrl), location).AbsoluteUri;
                response.Dispose();
                continue;
            }

            if (!response.IsSuccessStatusCode)
                return (null, string.Empty);

            var contentType = response.Content.Headers.ContentType?.ToString()
                              ?? "application/octet-stream";
            var stream = await response.Content.ReadAsStreamAsync(ct);
            return (stream, contentType);
        }

        return (null, string.Empty);
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

