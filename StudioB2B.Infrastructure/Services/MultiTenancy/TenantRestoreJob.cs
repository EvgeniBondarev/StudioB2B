using System.Diagnostics;
using System.IO.Compression;
using Hangfire;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Minio;
using Minio.DataModel.Args;
using StudioB2B.Domain.Entities;
using StudioB2B.Domain.Options;
using StudioB2B.Infrastructure.Persistence.Master;

namespace StudioB2B.Infrastructure.Services.MultiTenancy;

/// <summary>
/// Hangfire job для восстановления базы данных тенанта из бэкапа.
/// Запускается из MasterHangfireManager (master-restore queue).
/// Пайплайн: MinIO download -> temp .sql.gz -> GUnzip -> temp .sql -> mysql CLI -> cleanup.
/// </summary>
public class TenantRestoreJob
{
    private readonly MasterDbContext _masterDb;
    private readonly IMinioClient _minio;
    private readonly BackupOptions _options;
    private readonly ILogger<TenantRestoreJob> _logger;

    public TenantRestoreJob(
        MasterDbContext masterDb,
        IMinioClient minio,
        IOptions<BackupOptions> options,
        ILogger<TenantRestoreJob> logger)
    {
        _masterDb = masterDb;
        _minio = minio;
        _options = options.Value;
        _logger = logger;
    }

    [AutomaticRetry(Attempts = 0)]
    public async Task ExecuteAsync(
        Guid tenantId,
        string connectionString,
        string subdomain,
        string objectKey,
        string sourceType,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation(
            "TenantRestoreJob: starting restore for tenant {TenantId} ({Subdomain}) from {ObjectKey}.",
            tenantId, subdomain, objectKey);

        var history = new TenantRestoreHistory
        {
            TenantId = tenantId,
            SourceObjectKey = objectKey,
            SourceType = sourceType,
            Status = "Running",
            StartedAtUtc = DateTime.UtcNow
        };

        _masterDb.TenantRestoreHistories.Add(history);
        await _masterDb.SaveChangesAsync(cancellationToken);

        var tempGz = Path.Combine(Path.GetTempPath(), $"restore_{tenantId:N}_{DateTime.UtcNow:yyyyMMddHHmmss}.sql.gz");
        var tempSql = Path.ChangeExtension(tempGz, null);

        try
        {
            _logger.LogInformation("TenantRestoreJob: downloading {ObjectKey} from MinIO.", objectKey);

            await using (var fileStream = File.Create(tempGz))
            {
                var getArgs = new GetObjectArgs()
                    .WithBucket(_options.Bucket)
                    .WithObject(objectKey)
                    .WithCallbackStream(async (stream, innerCt) =>
                    {
                        await stream.CopyToAsync(fileStream, innerCt);
                    });

                await _minio.GetObjectAsync(getArgs, cancellationToken);
            }

            _logger.LogInformation("TenantRestoreJob: decompressing {File}.", tempGz);

            await using (var gzStream = new GZipStream(File.OpenRead(tempGz), CompressionMode.Decompress))
            await using (var sqlStream = File.Create(tempSql))
            {
                await gzStream.CopyToAsync(sqlStream, cancellationToken);
            }

            // Удаляем GTID_PURGED строки — они конфликтуют с уже выполненными
            // транзакциями на целевом сервере (ERROR 3546).
            await StripGtidLinesAsync(tempSql, cancellationToken);

            var (host, port, database, user, password) = ParseConnectionString(connectionString);

            _logger.LogInformation("TenantRestoreJob: running mysql restore for {Database}.", database);

            await RunMysqlAsync(_options.MysqlPath, host, port, database, user, password, tempSql, cancellationToken);

            history.Status = "Completed";
            history.CompletedAtUtc = DateTime.UtcNow;

            await _masterDb.SaveChangesAsync(cancellationToken);

            _logger.LogInformation("TenantRestoreJob: restore completed for tenant {TenantId}.", tenantId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "TenantRestoreJob: restore failed for tenant {TenantId}.", tenantId);

            history.Status = "Failed";
            history.ErrorMessage = ex.Message;
            history.CompletedAtUtc = DateTime.UtcNow;

            await _masterDb.SaveChangesAsync(CancellationToken.None);
            throw;
        }
        finally
        {
            DeleteTempFile(tempGz);
            DeleteTempFile(tempSql);

            if (sourceType == "Upload")
                await DeleteMinioObjectAsync(objectKey, CancellationToken.None);
        }
    }

    /// <summary>
    /// Убирает из SQL-файла строки, связанные с GTID_PURGED и SQL_LOG_BIN.
    /// mysqldump по умолчанию добавляет их, но они конфликтуют при восстановлении
    /// на сервер с уже существующими GTID-транзакциями (ERROR 3546).
    /// </summary>
    private static async Task StripGtidLinesAsync(string sqlFile, CancellationToken ct)
    {
        var tempOut = sqlFile + ".tmp";

        await using (var writer = new StreamWriter(tempOut, append: false))
        {
            using var reader = new StreamReader(sqlFile);
            string? line;
            while ((line = await reader.ReadLineAsync(ct)) is not null)
            {
                if (line.Contains("GTID_PURGED", StringComparison.OrdinalIgnoreCase) ||
                    line.Contains("SQL_LOG_BIN", StringComparison.OrdinalIgnoreCase) ||
                    line.Contains("MYSQLDUMP_TEMP_LOG_BIN", StringComparison.OrdinalIgnoreCase))
                    continue;

                await writer.WriteLineAsync(line.AsMemory(), ct);
            }
        }

        File.Move(tempOut, sqlFile, overwrite: true);
    }

    private static async Task RunMysqlAsync(
        string mysqlPath,
        string host, int port, string database,
        string user, string password,
        string sqlFile, CancellationToken ct)
    {
        var psi = new ProcessStartInfo
        {
            FileName = mysqlPath,
            Arguments = $"--protocol=TCP -h {host} -P {port} -u {user} {database}",
            RedirectStandardInput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true
        };

        psi.Environment["MYSQL_PWD"] = password;

        using var process = new Process { StartInfo = psi };
        process.Start();

        // Читаем stderr параллельно с записью stdin —
        // иначе, если mysql выйдет с ошибкой раньше чем мы дочитаем SQL,
        // получим Broken pipe и потеряем реальное сообщение об ошибке.
        var stderrTask = process.StandardError.ReadToEndAsync(ct);

        try
        {
            await using var sqlStream = File.OpenRead(sqlFile);
            await sqlStream.CopyToAsync(process.StandardInput.BaseStream, ct);
        }
        catch (IOException)
        {
            // mysql завершился раньше (ошибка подключения / SQL-ошибка) —
            // реальная причина будет в stderr и exit code ниже.
        }
        finally
        {
            try { process.StandardInput.Close(); } catch { /* ignore */ }
        }

        var stderr = await stderrTask;
        await process.WaitForExitAsync(ct);

        if (process.ExitCode != 0)
            throw new InvalidOperationException(
                $"mysql завершился с кодом {process.ExitCode}: {stderr.Trim()}");
    }

    private async Task DeleteMinioObjectAsync(string objectKey, CancellationToken ct)
    {
        try
        {
            var removeArgs = new RemoveObjectArgs()
                .WithBucket(_options.Bucket)
                .WithObject(objectKey);

            await _minio.RemoveObjectAsync(removeArgs, ct);

            _logger.LogInformation("TenantRestoreJob: deleted temporary upload object {Key}.", objectKey);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "TenantRestoreJob: failed to delete MinIO object {Key}.", objectKey);
        }
    }

    private void DeleteTempFile(string path)
    {
        if (!File.Exists(path)) return;
        try
        {
            File.Delete(path);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "TenantRestoreJob: failed to delete temp file {File}.", path);
        }
    }

    private static (string Host, int Port, string Database, string User, string Password) ParseConnectionString(string cs)
    {
        var dict = cs.Split(';', StringSplitOptions.RemoveEmptyEntries)
            .Select(part => part.Split('=', 2))
            .Where(parts => parts.Length == 2)
            .ToDictionary(
                parts => parts[0].Trim(),
                parts => parts[1].Trim(),
                StringComparer.OrdinalIgnoreCase);

        dict.TryGetValue("Server", out var host);
        host ??= "localhost";

        dict.TryGetValue("Port", out var portStr);
        var port = int.TryParse(portStr, out var p) ? p : 3306;

        dict.TryGetValue("Database", out var database);
        database ??= "";

        if (!dict.TryGetValue("User", out var user))
            dict.TryGetValue("User Id", out user);
        user ??= "root";

        dict.TryGetValue("Password", out var password);
        password ??= "";

        return (host, port, database, user, password);
    }
}
