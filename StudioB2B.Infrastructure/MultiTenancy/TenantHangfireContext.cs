using Hangfire;
using Hangfire.MySql;

namespace StudioB2B.Infrastructure.MultiTenancy;

/// <summary>
/// Обёртка над Hangfire-ресурсами одного тенанта.
/// </summary>
public sealed class TenantHangfireContext : IDisposable
{
    public IBackgroundJobClient Client { get; }
    public BackgroundJobServer Server { get; }
    public MySqlStorage Storage { get; }

    public TenantHangfireContext(
        IBackgroundJobClient client,
        BackgroundJobServer server,
        MySqlStorage storage)
    {
        Client  = client;
        Server  = server;
        Storage = storage;
    }

    public void Dispose()
    {
        Server.Dispose();
        Storage.Dispose();
    }
}

