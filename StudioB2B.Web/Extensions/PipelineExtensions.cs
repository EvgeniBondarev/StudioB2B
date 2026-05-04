using Microsoft.EntityFrameworkCore;
using Serilog;
using StudioB2B.Infrastructure.Interfaces;
using StudioB2B.Infrastructure.Persistence.Master;
using StudioB2B.Infrastructure.Services.MultiTenancy;
using StudioB2B.Web.Components;
using StudioB2B.Web.Hubs;

namespace StudioB2B.Web.Extensions;

public static partial class PipelineExtensions
{
    public static WebApplication ConfigurePipeline(this WebApplication app)
    {
        app.UseSerilogRequestLogging();

        // Перехват ошибок только для не-API маршрутов.
        // API эндпоинты возвращают JSON/plain-text статусы напрямую;
        // re-execute для них приводит к ошибке "Incorrect Content-Type" в Blazor.
        app.UseWhen(
            ctx => !ctx.Request.Path.StartsWithSegments("/api"),
            a => a.UseStatusCodePagesWithReExecute("/not-found", createScopeForStatusCodePages: true));

        app.UseHttpsRedirection();

        app.UseForwardedHeaders();
        app.UseCors("AllowSubdomains");
        app.UseMiddleware<TenantMiddleware>();
        app.UseAuthentication();
        app.UseAuthorization();
        app.Use(async (ctx, next) =>
        {
            await next();

            if (!ctx.Request.Path.StartsWithSegments("/api"))
                return;

            if (ctx.Response.StatusCode is not (StatusCodes.Status401Unauthorized or StatusCodes.Status403Forbidden))
                return;
        });

        app.Use((ctx, next) =>
        {
            var ct = ctx.Request.ContentType;
            if (ct != null && ct.StartsWith("multipart/form-data", StringComparison.OrdinalIgnoreCase))
            {
                var match = BoundaryRegex().Match(ct);
                if (match.Success)
                {
                    var raw = match.Groups[1].Value.TrimEnd(';');
                    // tspecials per RFC 2045: ()<>@,;:\"/[]?=
                    if (raw.IndexOfAny(['(', ')', '<', '>', '@', ',', ';', ':', '\\', '"', '/', '[', ']', '?', '=', ' ']) >= 0)
                        ctx.Request.ContentType = ct.Replace($"boundary={raw}", $"boundary=\"{raw}\"");
                }
            }

            return next(ctx);
        });

        // Обязателен для Blazor Interactive Server.
        // IAntiforgery заменён на NoOpAntiforgery — реальная валидация отключена.
        app.UseAntiforgery();

        app.MapControllers();
        app.UseStaticFiles();
        app.MapStaticAssets();

        app.MapHub<SyncNotificationHub>("/hubs/sync");
        app.MapHub<TaskBoardHub>("/hubs/taskboard");
        app.MapHub<OzonPushHub>("/hubs/ozon-push");

        app.MapRazorComponents<App>()
            .AddInteractiveServerRenderMode()
            .AllowAnonymous();

        // Применяем pending миграции для всех существующих тенантов при старте
        _ = Task.Run(() => MigrateAllTenantsAsync(app));

        return app;
    }

    private static async Task MigrateAllTenantsAsync(WebApplication app)
    {
        try
        {
            await using var scope = app.Services.CreateAsyncScope();
            var masterDb = scope.ServiceProvider.GetRequiredService<MasterDbContext>();
            var initializer = scope.ServiceProvider.GetRequiredService<ITenantDatabaseInitializer>();
            var logger = scope.ServiceProvider.GetRequiredService<ILogger<WebApplication>>();

            var tenants = await masterDb.Tenants
                .Select(t => new { t.Id, t.ConnectionString, t.Subdomain })
                .ToListAsync();

            logger.LogInformation("Startup: migrating {Count} tenant database(s).", tenants.Count);

            foreach (var tenant in tenants)
            {
                try
                {
                    await initializer.MigrateOnlyAsync(tenant.ConnectionString, tenant.Subdomain, CancellationToken.None);
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

    [System.Text.RegularExpressions.GeneratedRegex(@"boundary=(?!"")(\S+)", System.Text.RegularExpressions.RegexOptions.IgnoreCase, "en-BY")]
    private static partial System.Text.RegularExpressions.Regex BoundaryRegex();
}
