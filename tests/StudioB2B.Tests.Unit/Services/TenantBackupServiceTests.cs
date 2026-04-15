using FluentAssertions;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;
using Minio;
using Moq;
using StudioB2B.Domain.Options;
using StudioB2B.Infrastructure.Services;
using Xunit;

namespace StudioB2B.Tests.Unit.Services;

/// <summary>
/// Tests for TenantBackupService focusing on the download-token cache mechanism,
/// which is the only logic that doesn't require MinIO or Hangfire.
/// </summary>
public class TenantBackupServiceTests
{
    private static TenantBackupService CreateService(IMemoryCache cache)
        => new(
            masterDb: null!,
            hangfireManager: null!,
            minio: Mock.Of<IMinioClient>(),
            options: new OptionsWrapper<BackupOptions>(new BackupOptions()),
            cache: cache,
            mapper: Mock.Of<AutoMapper.IMapper>());

    private static MemoryCache NewCache() => new(new MemoryCacheOptions());

    // Simulate GenerateDownloadToken by writing directly to the cache with the same key prefix
    private static string StoreToken(IMemoryCache cache, string objectKey, string fileName, long? size = null)
    {
        const string prefix = "backup_dl_";
        var token = Guid.NewGuid().ToString("N");
        cache.Set(prefix + token, (objectKey, fileName, size), TimeSpan.FromMinutes(15));
        return token;
    }

    [Fact]
    public void ConsumeDownloadToken_ValidToken_ReturnsEntry()
    {
        var cache = NewCache();
        var svc = CreateService(cache);
        var token = StoreToken(cache, "backups/db.sql.gz", "db.sql.gz", 1024);

        var result = svc.ConsumeDownloadToken(token);

        result.Should().NotBeNull();
        result.Value.ObjectKey.Should().Be("backups/db.sql.gz");
        result.Value.FileName.Should().Be("db.sql.gz");
        result.Value.SizeBytes.Should().Be(1024);
    }

    [Fact]
    public void ConsumeDownloadToken_UnknownToken_ReturnsNull()
    {
        var svc = CreateService(NewCache());

        var result = svc.ConsumeDownloadToken(Guid.NewGuid().ToString("N"));

        result.Should().BeNull();
    }

    [Fact]
    public void ConsumeDownloadToken_TokenIsOneTimeUse_SecondCallReturnsNull()
    {
        var cache = NewCache();
        var svc = CreateService(cache);
        var token = StoreToken(cache, "backups/db.sql.gz", "db.sql.gz");

        var first = svc.ConsumeDownloadToken(token);
        var second = svc.ConsumeDownloadToken(token);

        first.Should().NotBeNull("first consume must succeed");
        second.Should().BeNull("token must be removed after first consume");
    }

    [Fact]
    public void ConsumeDownloadToken_EmptyToken_ReturnsNull()
    {
        var svc = CreateService(NewCache());

        var result = svc.ConsumeDownloadToken("");

        result.Should().BeNull();
    }
}
