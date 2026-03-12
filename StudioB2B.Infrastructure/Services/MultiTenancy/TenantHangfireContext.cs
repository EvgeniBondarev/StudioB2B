using Hangfire;
using Hangfire.MySql;

namespace StudioB2B.Infrastructure.Services.MultiTenancy;

/// <summary>
/// Обёртка над Hangfire-ресурсами одного тенанта.
/// </summary>
public sealed class TenantHangfireContext : IDisposable
{
    public IBackgroundJobClient Client { get; }

    public BackgroundJobServer Server { get; }

    public MySqlStorage Storage { get; }

    public RecurringJobManager RecurringJobManager { get; }

    public TenantHangfireContext(IBackgroundJobClient client, BackgroundJobServer server, MySqlStorage storage,
                                 RecurringJobManager recurringJobManager)
    {
        Client = client;
        Server = server;
        Storage = storage;
        RecurringJobManager = recurringJobManager;
    }

    public void Dispose()
    {
        Server.Dispose();
        Storage.Dispose();
    }
}
