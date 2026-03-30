using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using StudioB2B.Infrastructure.Interfaces;
using StudioB2B.Shared;

namespace StudioB2B.Web.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class MarketplaceClientsController : ControllerBase
{
    private readonly IMarketplaceClientService _marketplaceClientService;
    private readonly ITenantProvider _tenantProvider;

    public MarketplaceClientsController(
        IMarketplaceClientService marketplaceClientService,
        ITenantProvider tenantProvider)
    {
        _marketplaceClientService = marketplaceClientService;
        _tenantProvider = tenantProvider;
    }

    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        if (!_tenantProvider.IsResolved)
            return BadRequest(new { error = "Tenant not resolved" });

        var list = await _marketplaceClientService.GetAllAsync();
        return Ok(list);
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> Get(Guid id)
    {
        if (!_tenantProvider.IsResolved)
            return BadRequest(new { error = "Tenant not resolved" });

        var item = await _marketplaceClientService.GetByIdAsync(id);
        if (item == null) return NotFound();
        return Ok(item);
    }

    [HttpPost]
    public async Task<IActionResult> Create(CreateMarketplaceClientDto req)
    {
        if (!_tenantProvider.IsResolved)
            return BadRequest(new { error = "Tenant not resolved" });

        var dto = await _marketplaceClientService.CreateAsync(req);
        return CreatedAtAction(nameof(Get), new { id = dto.Id }, dto);
    }

    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Update(Guid id, UpdateMarketplaceClientDto req)
    {
        if (!_tenantProvider.IsResolved)
            return BadRequest(new { error = "Tenant not resolved" });
        if (id != req.Id) return BadRequest();

        var updated = await _marketplaceClientService.UpdateAsync(req);
        if (updated == null) return NotFound();
        return NoContent();
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id)
    {
        if (!_tenantProvider.IsResolved)
            return BadRequest(new { error = "Tenant not resolved" });

        var deleted = await _marketplaceClientService.DeleteAsync(id);
        if (!deleted) return NotFound();
        return NoContent();
    }
}
