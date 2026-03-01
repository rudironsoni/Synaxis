using Billing.Application.DTOs;
using Billing.Application.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Billing.Api.Controllers;

[ApiController]
[Route("api/v1/[controller]")]
[Authorize]
public class SubscriptionsController : ControllerBase
{
    private readonly ISubscriptionService _subscriptionService;

    public SubscriptionsController(ISubscriptionService subscriptionService)
    {
        _subscriptionService = subscriptionService ?? throw new ArgumentNullException(nameof(subscriptionService));
    }

    [HttpGet]
    [ProducesResponseType(typeof(SubscriptionDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetSubscription(CancellationToken cancellationToken)
    {
        var organizationId = GetOrganizationId();
        var subscription = await _subscriptionService.GetSubscriptionAsync(organizationId, cancellationToken);
        
        if (subscription == null)
            return NotFound();
        
        return Ok(subscription);
    }

    [HttpPost]
    [Authorize(Roles = "Admin")]
    [ProducesResponseType(typeof(SubscriptionDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> CreateSubscription(
        [FromBody] CreateSubscriptionRequest request,
        CancellationToken cancellationToken)
    {
        var organizationId = GetOrganizationId();
        var subscriptionRequest = request with { OrganizationId = organizationId };
        
        try
        {
            var subscription = await _subscriptionService.CreateSubscriptionAsync(subscriptionRequest, cancellationToken);
            return CreatedAtAction(nameof(GetSubscription), subscription);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }

    [HttpPost("cancel")]
    [Authorize(Roles = "Admin")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> CancelSubscription(
        [FromBody] CancelSubscriptionRequest request,
        CancellationToken cancellationToken)
    {
        var organizationId = GetOrganizationId();
        var result = await _subscriptionService.CancelSubscriptionAsync(organizationId, request.Reason, cancellationToken);
        
        if (!result)
            return NotFound();
        
        return NoContent();
    }

    [HttpPost("upgrade")]
    [Authorize(Roles = "Admin")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpgradeSubscription(
        [FromBody] UpgradeSubscriptionRequest request,
        CancellationToken cancellationToken)
    {
        var organizationId = GetOrganizationId();
        var result = await _subscriptionService.UpgradeSubscriptionAsync(organizationId, request.NewPlanId, cancellationToken);
        
        if (!result)
            return NotFound();
        
        return NoContent();
    }

    private Guid GetOrganizationId()
    {
        var claim = User.FindFirst("organization_id")?.Value;
        return claim != null ? Guid.Parse(claim) : Guid.Empty;
    }
}

public record CancelSubscriptionRequest(string Reason);
public record UpgradeSubscriptionRequest(string NewPlanId);
