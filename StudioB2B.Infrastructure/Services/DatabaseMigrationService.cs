using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using StudioB2B.Infrastructure.Persistence.Master;

namespace StudioB2B.Infrastructure.Services;

/// <summary>
/// Сервис автоматического применения миграций при запуске (IHostedService)
/// </summary>
public class DatabaseMigrationService : IHostedService
{
    private readonly IServiceProvider _serviceProvider;

    public DatabaseMigrationService(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        await using var scope = _serviceProvider.CreateAsyncScope();
        var masterDbContext = scope.ServiceProvider.GetRequiredService<MasterDbContext>();

        var pendingMigrations = await masterDbContext.Database
                                    .GetPendingMigrationsAsync(cancellationToken);

        var migrations = pendingMigrations.ToList();
        if (migrations.Count > 0)
            await masterDbContext.Database.MigrateAsync(cancellationToken);
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}
