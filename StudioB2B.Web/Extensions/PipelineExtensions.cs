using Microsoft.EntityFrameworkCore;
using Serilog;
using StudioB2B.Infrastructure.MultiTenancy.Initialization;
using StudioB2B.Infrastructure.Persistence.Master;
using StudioB2B.Infrastructure.MultiTenancy.Middleware;
using StudioB2B.Web.Components;
using StudioB2B.Web.Hubs;

namespace StudioB2B.Web.Extensions;

public static class PipelineExtensions
{
    public static WebApplication ConfigurePipeline(this WebApplication app)
    {
        app.UseSerilogRequestLogging();

        app.UseStatusCodePagesWithReExecute("/not-found", createScopeForStatusCodePages: true);
        app.UseHttpsRedirection();

        app.UseForwardedHeaders();
        app.UseCors("AllowSubdomains");
        app.UseMiddleware<TenantMiddleware>();
        app.UseAuthentication();
        app.UseAuthorization();
        // Обязателен для Blazor Interactive Server.
        // IAntiforgery заменён на NoOpAntiforgery — реальная валидация отключена.
        app.UseAntiforgery();

        app.MapControllers();
        app.MapStaticAssets();

        app.MapHub<SyncNotificationHub>("/hubs/sync");

        app.MapRazorComponents<App>()
            .AddInteractiveServerRenderMode();

        // Применяем pending миграции для всех существующих тенантов при старте
        _ = Task.Run(() => MigrateAllTenantsAsync(app));

        return app;
    }

    private static async Task MigrateAllTenantsAsync(WebApplication app)
    {
        try
        {
            await using var scope = app.Services.CreateAsyncScope();
            var masterDb    = scope.ServiceProvider.GetRequiredService<MasterDbContext>();
            var initializer = scope.ServiceProvider.GetRequiredService<ITenantDatabaseInitializer>();
            var logger      = scope.ServiceProvider.GetRequiredService<ILogger<WebApplication>>();

            var tenants = await masterDb.Tenants
                .Select(t => new { t.Id, t.ConnectionString })
                .ToListAsync();

            logger.LogInformation("Startup: migrating {Count} tenant database(s).", tenants.Count);

            foreach (var tenant in tenants)
            {
                try
                {
                    await initializer.MigrateOnlyAsync(tenant.ConnectionString, CancellationToken.None);
                }
                catch (Exception ex)
                {
                    logger.LogError(ex, "Startup: failed to migrate tenant {TenantId}.", tenant.Id);
                }
            }

            logger.LogInformation("Startup: all tenant migrations completed.");
        }
        catch (Exception ex)
        {
            var logger = app.Services.GetRequiredService<ILogger<WebApplication>>();
            logger.LogError(ex, "Startup: tenant migration runner failed.");
        }
    }
}
