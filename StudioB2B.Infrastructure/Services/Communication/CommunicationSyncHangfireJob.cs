using Hangfire;
using StudioB2B.Infrastructure.Interfaces;
using StudioB2B.Infrastructure.Services.MultiTenancy;

namespace StudioB2B.Infrastructure.Services.Communication;

/// <summary>
/// Thin Hangfire entry-point for communication sync.
/// Initialises TenantProvider in the DI scope before delegating to ICommunicationTaskSyncService.
/// </summary>
public class CommunicationSyncHangfireJob
{
    private readonly TenantProvider _tenantProvider;
    private readonly ICommunicationTaskSyncService _syncService;

    public CommunicationSyncHangfireJob(TenantProvider tenantProvider, ICommunicationTaskSyncService syncService)
    {
        _tenantProvider = tenantProvider;
        _syncService = syncService;
    }

    [AutomaticRetry(Attempts = 0)]
    public async Task RunAsync(Guid tenantId, string connectionString, CancellationToken ct = default)
    {
        _tenantProvider.SetTenant(tenantId, connectionString);
        await _syncService.SyncAsync(ct);
    }

    [AutomaticRetry(Attempts = 0)]
    public async Task UpsertChatAsync(
        Guid tenantId, string connectionString,
        string chatId, string messageType,
        CancellationToken ct = default)
    {
        _tenantProvider.SetTenant(tenantId, connectionString);
        await _syncService.UpsertChatAsync(chatId, messageType, ct);
    }
}

