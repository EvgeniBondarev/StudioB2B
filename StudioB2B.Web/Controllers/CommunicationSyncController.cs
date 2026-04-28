using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using StudioB2B.Infrastructure.Interfaces;
using StudioB2B.Infrastructure.Services.Communication;

namespace StudioB2B.Web.Controllers;

[ApiController]
[Route("api/communication/sync")]
[Authorize]
public class CommunicationSyncController : ControllerBase
{
    private readonly CommunicationTaskSyncJob _syncJob;
    private readonly ITenantProvider _tenantProvider;

    public CommunicationSyncController(CommunicationTaskSyncJob syncJob, ITenantProvider tenantProvider)
    {
        _syncJob = syncJob;
        _tenantProvider = tenantProvider;
    }

    [HttpPost]
    public async Task<IActionResult> Sync(CancellationToken ct)
    {
        if (!_tenantProvider.IsResolved)
            return BadRequest(new { error = "Tenant not resolved" });

        await _syncJob.ExecuteAsync(_tenantProvider.TenantId!.Value, _tenantProvider.ConnectionString!, ct);
        return Ok(new { success = true });
    }
}

