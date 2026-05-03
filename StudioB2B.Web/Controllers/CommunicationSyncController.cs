using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using StudioB2B.Infrastructure.Interfaces;

namespace StudioB2B.Web.Controllers;

[ApiController]
[Route("api/communication/sync")]
[Authorize]
public class CommunicationSyncController : ControllerBase
{
    private readonly ICommunicationTaskSyncService _syncService;
    private readonly ITenantProvider _tenantProvider;

    public CommunicationSyncController(ICommunicationTaskSyncService syncService, ITenantProvider tenantProvider)
    {
        _syncService = syncService;
        _tenantProvider = tenantProvider;
    }

    [HttpPost]
    public async Task<IActionResult> Sync(CancellationToken ct)
    {
        if (!_tenantProvider.IsResolved)
            return BadRequest(new { error = "Tenant not resolved" });

        await _syncService.SyncAsync(ct);
        return Ok(new { success = true });
    }
}

