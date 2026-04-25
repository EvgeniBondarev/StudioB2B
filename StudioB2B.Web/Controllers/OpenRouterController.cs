using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using StudioB2B.Infrastructure.Interfaces;
using StudioB2B.Shared;

namespace StudioB2B.Web.Controllers;

[ApiController]
[Route("api/open-router")]
public class OpenRouterController : ControllerBase
{
    private readonly IOpenRouterService _openRouterService;
    private readonly ITenantProvider _tenantProvider;

    public OpenRouterController(IOpenRouterService openRouterService, ITenantProvider tenantProvider)
    {
        _openRouterService = openRouterService;
        _tenantProvider = tenantProvider;
    }

    [HttpPost("chat")]
    public async Task<IActionResult> Chat([FromBody] OpenRouterChatRequestDto request, CancellationToken ct = default)
    {
        if (!_tenantProvider.IsResolved)
            return BadRequest(new { error = "Tenant not resolved" });

        if (string.IsNullOrWhiteSpace(request.Message))
            return BadRequest(new { error = "Message is required" });

        var response = await _openRouterService.ChatAsync(request, ct);
        return Ok(response);
    }

    [HttpPost("suggest-reply")]
    public async Task<IActionResult> SuggestReply([FromBody] OpenRouterSuggestReplyRequestDto request, CancellationToken ct = default)
    {
        if (!_tenantProvider.IsResolved)
            return BadRequest(new { error = "Tenant not resolved" });

        if (string.IsNullOrWhiteSpace(request.Context))
            return BadRequest(new { error = "Context is required" });

        var response = await _openRouterService.SuggestReplyAsync(request, ct);
        return Ok(response);
    }
}
