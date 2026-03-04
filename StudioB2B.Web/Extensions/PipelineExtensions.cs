using Serilog;
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
        app.UseAntiforgery();

        app.MapControllers();
        app.MapStaticAssets();

        // SignalR Hub для уведомлений о завершении задач — до MapRazorComponents
        app.MapHub<SyncNotificationHub>("/hubs/sync");

        app.MapRazorComponents<App>()
            .AddInteractiveServerRenderMode();

        return app;
    }
}
