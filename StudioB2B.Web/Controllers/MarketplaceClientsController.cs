using AutoMapper;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using StudioB2B.Application.Common.Interfaces;
using StudioB2B.Infrastructure.Persistence.Tenant;
using StudioB2B.Infrastructure.Extensions;
using StudioB2B.Infrastructure.Features.Marketplace;
using StudioB2B.Infrastructure.Services;
using StudioB2B.Shared.DTOs;

namespace StudioB2B.Web.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class MarketplaceClientsController : ControllerBase
{
    private readonly ITenantDbContextFactory _dbContextFactory;
    private readonly ITenantProvider _tenantProvider;
    private readonly ILogger<MarketplaceClientsController> _logger;
    private readonly IMapper _mapper;

    public MarketplaceClientsController(
        ITenantDbContextFactory dbContextFactory,
        ITenantProvider tenantProvider,
        ILogger<MarketplaceClientsController> logger,
        IMapper mapper)
    {
        _dbContextFactory = dbContextFactory;
        _tenantProvider = tenantProvider;
        _logger = logger;
        _mapper = mapper;
    }

    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        if (!_tenantProvider.IsResolved)
            return BadRequest(new { error = "Tenant not resolved" });

        using var db = _dbContextFactory.CreateDbContext();
        var list = await db.MarketplaceClients!.GetAllAsync(_mapper);
        return Ok(list);
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> Get(Guid id)
    {
        if (!_tenantProvider.IsResolved)
            return BadRequest(new { error = "Tenant not resolved" });

        using var db = _dbContextFactory.CreateDbContext();
        var item = await db.MarketplaceClients!.GetByIdAsync(id, _mapper);
        if (item == null) return NotFound();
        return Ok(item);
    }

    [HttpPost]
    public async Task<IActionResult> Create(CreateMarketplaceClientRequest req)
    {
        if (!_tenantProvider.IsResolved)
            return BadRequest(new { error = "Tenant not resolved" });

        using var db = _dbContextFactory.CreateDbContext();
        var dto = await db.CreateAsync(req, _mapper);
        return CreatedAtAction(nameof(Get), new { id = dto.Id }, dto);
    }

    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Update(Guid id, UpdateMarketplaceClientRequest req)
    {
        if (!_tenantProvider.IsResolved)
            return BadRequest(new { error = "Tenant not resolved" });
        if (id != req.Id) return BadRequest();

        using var db = _dbContextFactory.CreateDbContext();
        var updated = await db.UpdateAsync(req, _mapper);
        if (updated == null) return NotFound();
        return NoContent();
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id)
    {
        if (!_tenantProvider.IsResolved)
            return BadRequest(new { error = "Tenant not resolved" });

        using var db = _dbContextFactory.CreateDbContext();
        var deleted = await db.DeleteAsync(id);
        if (!deleted) return NotFound();
        return NoContent();
    }
}
