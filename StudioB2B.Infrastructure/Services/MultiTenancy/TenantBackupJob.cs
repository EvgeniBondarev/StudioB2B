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
/// Hangfire job для создания бэкапа базы данных одного тенанта.
/// Запускается из MasterHangfireManager (master queue).
/// Пайплайн: mysqldump → GZipStream → temp file → MinIO upload → cleanup.
/// </summary>
public class TenantBackupJob
{
    private readonly MasterDbContext _masterDb;
    private readonly IMinioClient _minio;
    private readonly BackupOptions _options;
    private readonly ILogger<TenantBackupJob> _logger;

    public TenantBackupJob(
        MasterDbContext masterDb,
        IMinioClient minio,
        IOptions<BackupOptions> options,
        ILogger<TenantBackupJob> logger)
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
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("TenantBackupJob: starting backup for tenant {TenantId} ({Subdomain}).", tenantId, subdomain);

        var history = new TenantBackupHistory
        {
            TenantId = tenantId,
            Status = "Running",
            StartedAtUtc = DateTime.UtcNow
        };

        _masterDb.TenantBackupHistories.Add(history);
        await _masterDb.SaveChangesAsync(cancellationToken);

        var tempFile = Path.Combine(Path.GetTempPath(), $"backup_{tenantId:N}_{DateTime.UtcNow:yyyyMMddHHmmss}.sql.gz");

        try
        {
            await EnsureBucketExistsAsync(cancellationToken);

            var (host, port, database, user, password) = ParseConnectionString(connectionString);

            _logger.LogInformation("TenantBackupJob: running mysqldump for {Database}.", database);

            await RunMysqldumpAsync(_options.MysqldumpPath, host, port, database, user, password, tempFile, cancellationToken);

            var fileInfo = new FileInfo(tempFile);
            var objectKey = $"{subdomain}/{history.Id}.sql.gz";

            _logger.LogInformation("TenantBackupJob: uploading {Size} bytes to MinIO key {Key}.", fileInfo.Length, objectKey);

            await using (var fileStream = File.OpenRead(tempFile))
            {
                var putArgs = new PutObjectArgs()
                    .WithBucket(_options.Bucket)
                    .WithObject(objectKey)
                    .WithStreamData(fileStream)
                    .WithObjectSize(fileInfo.Length)
                    .WithContentType("application/gzip");

                await _minio.PutObjectAsync(putArgs, cancellationToken);
            }

            history.MinioObjectKey = objectKey;
            history.SizeBytes = fileInfo.Length;
            history.Status = "Completed";
            history.CompletedAtUtc = DateTime.UtcNow;

            await _masterDb.SaveChangesAsync(cancellationToken);

            _logger.LogInformation("TenantBackupJob: backup completed for tenant {TenantId}.", tenantId);

            await ApplyRetentionAsync(tenantId, subdomain, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "TenantBackupJob: backup failed for tenant {TenantId}.", tenantId);

            history.Status = "Failed";
            history.ErrorMessage = ex.Message;
            history.CompletedAtUtc = DateTime.UtcNow;

            await _masterDb.SaveChangesAsync(CancellationToken.None);
            throw;
        }
        finally
        {
            if (File.Exists(tempFile))
            {
                try { File.Delete(tempFile); }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "TenantBackupJob: failed to delete temp file {File}.", tempFile);
                }
            }
        }
    }

    private static async Task RunMysqldumpAsync(
        string mysqldumpPath,
        string host, int port, string database,
        string user, string password,
        string outputFile, CancellationToken ct)
    {
        var psi = new ProcessStartInfo
        {
            FileName = mysqldumpPath,
            Arguments = $"--protocol=TCP --single-transaction --quick --skip-lock-tables --set-gtid-purged=OFF -h {host} -P {port} -u {user} {database}",
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true,
            Environment = { ["MYSQL_PWD"] = password }
        };

        using var process = new Process();
        process.StartInfo = psi;
        process.Start();

        await using (var fileStream = File.Create(outputFile))
        await using (var gzip = new GZipStream(fileStream, CompressionLevel.Optimal))
        {
            await process.StandardOutput.BaseStream.CopyToAsync(gzip, ct);
        }

        var stderr = await process.StandardError.ReadToEndAsync(ct);
        await process.WaitForExitAsync(ct);

        if (process.ExitCode != 0)
            throw new InvalidOperationException($"mysqldump exited with code {process.ExitCode}: {stderr}");
    }

    private async Task EnsureBucketExistsAsync(CancellationToken ct)
    {
        var bucketExistsArgs = new BucketExistsArgs().WithBucket(_options.Bucket);
        var exists = await _minio.BucketExistsAsync(bucketExistsArgs, ct);

        if (!exists)
        {
            var makeBucketArgs = new MakeBucketArgs().WithBucket(_options.Bucket);
            await _minio.MakeBucketAsync(makeBucketArgs, ct);
            _logger.LogInformation("TenantBackupJob: created MinIO bucket '{Bucket}'.", _options.Bucket);
        }
    }

    private async Task ApplyRetentionAsync(Guid tenantId, string subdomain, CancellationToken ct)
    {
        try
        {
            var schedule = await _masterDb.TenantBackupSchedules
                .AsNoTracking()
                .FirstOrDefaultAsync(s => s.TenantId == tenantId, ct);

            var retentionDays = schedule?.RetentionDays ?? _options.DefaultRetentionDays;
            var cutoff = DateTime.UtcNow.AddDays(-retentionDays);

            var oldRecords = await _masterDb.TenantBackupHistories
                .Where(h => h.TenantId == tenantId
                         && h.Status == "Completed"
                         && h.StartedAtUtc < cutoff
                         && h.MinioObjectKey != null)
                .ToListAsync(ct);

            if (oldRecords.Count == 0) return;

            foreach (var record in oldRecords)
            {
                try
                {
                    var removeArgs = new RemoveObjectArgs()
                        .WithBucket(_options.Bucket)
                        .WithObject(record.MinioObjectKey!);

                    await _minio.RemoveObjectAsync(removeArgs, ct);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "TenantBackupJob: failed to delete MinIO object {Key}.", record.MinioObjectKey);
                }
            }

            _masterDb.TenantBackupHistories.RemoveRange(oldRecords);
            await _masterDb.SaveChangesAsync(ct);

            _logger.LogInformation("TenantBackupJob: removed {Count} old backup(s) for tenant {TenantId} (retention={Days}d).",
                oldRecords.Count, tenantId, retentionDays);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "TenantBackupJob: retention cleanup failed for tenant {TenantId}.", tenantId);
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
