using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using StudioB2B.Infrastructure.Interfaces;

namespace StudioB2B.Web.Controllers;

/// <summary>
/// Проксирует скачивание бэкапов из MinIO через приложение.
/// Авторизация обеспечивается одноразовым download-токеном, который выдаётся
/// только аутентифицированному Admin-пользователю в Blazor-компоненте.
/// </summary>
[ApiController]
[Route("api/backup")]
public class BackupController : ControllerBase
{
    private readonly ITenantBackupService _backupService;

    public BackupController(ITenantBackupService backupService)
    {
        _backupService = backupService;
    }

    /// <summary>
    /// GET /api/backup/download?token=...
    /// Проверяет одноразовый токен и стримит файл бэкапа из MinIO напрямую в браузер.
    /// </summary>
    [HttpGet("download")]
    [AllowAnonymous]
    public async Task Download([FromQuery] string token, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(token))
        {
            Response.StatusCode = 400;
            return;
        }

        var info = _backupService.ConsumeDownloadToken(token);
        if (info is null)
        {
            Response.StatusCode = 404;
            return;
        }

        var fileName = string.IsNullOrWhiteSpace(info.Value.FileName)
            ? "backup.sql.gz"
            : info.Value.FileName;

        Response.ContentType = "application/gzip";
        Response.Headers.ContentDisposition =
            $"attachment; filename*=UTF-8''{Uri.EscapeDataString(fileName)}";

        if (info.Value.SizeBytes.HasValue)
            Response.ContentLength = info.Value.SizeBytes.Value;

        await _backupService.StreamObjectAsync(info.Value.ObjectKey, Response.Body, ct);
    }
}

