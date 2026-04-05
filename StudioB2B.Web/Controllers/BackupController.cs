using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using StudioB2B.Infrastructure.Interfaces;

namespace StudioB2B.Web.Controllers;

/// <summary>
/// Проксирует скачивание бэкапов из MinIO через приложение.
/// Авторизация обеспечивается одноразовым download-токеном, который выдаётся
/// только аутентифицированному Admin-пользователю в Blazor-компоненте.
/// Восстановление требует роль Admin напрямую.
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

    /// <summary>
    /// POST /api/backup/restore/history/{historyId}?tenantId=...
    /// Ставит в очередь восстановление из сохранённого бэкапа.
    /// </summary>
    [HttpPost("restore/history/{historyId:guid}")]
    [Authorize]
    public async Task<IActionResult> RestoreFromHistory(
        Guid historyId,
        [FromQuery] Guid tenantId,
        CancellationToken ct)
    {
        await _backupService.EnqueueRestoreAsync(tenantId, historyId, ct);
        return Ok();
    }

    /// <summary>
    /// POST /api/backup/restore/upload?tenantId=...
    /// Принимает .sql.gz файл потоком (без буферизации на диск),
    /// загружает в MinIO и ставит в очередь восстановление.
    /// </summary>
    [HttpPost("restore/upload")]
    [Authorize]
    [DisableRequestSizeLimit]
    public async Task<IActionResult> UploadAndRestore(
        [FromQuery] Guid tenantId,
        CancellationToken ct)
    {
        var objectKey = $"uploads/{tenantId:N}/{Guid.NewGuid():N}.sql.gz";

        await _backupService.UploadToMinioAsync(objectKey, Request.Body, Request.ContentLength, ct);
        await _backupService.EnqueueRestoreAsync(tenantId, objectKey, "Upload", ct);

        return Ok();
    }
}

