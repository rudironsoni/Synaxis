using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Synaxis.InferenceGateway.Application.ApiKeys;
using Synaxis.InferenceGateway.Application.ApiKeys.Models;
using System.Security.Claims;

namespace Synaxis.InferenceGateway.WebApi.Endpoints;

/// <summary>
/// API endpoints for API key management.
/// </summary>
[ApiController]
[Route("apikeys")]
[Authorize]
public class ApiKeyEndpoints : ControllerBase
{
    private readonly IApiKeyService _apiKeyService;

    /// <summary>
    /// Initializes a new instance of the <see cref="ApiKeyEndpoints"/> class.
    /// </summary>
    public ApiKeyEndpoints(IApiKeyService apiKeyService)
    {
        _apiKeyService = apiKeyService;
    }

    /// <summary>
    /// Generates a new API key for the current user's organization.
    /// POST /apikeys
    /// </summary>
    [HttpPost]
    public async Task<IActionResult> GenerateApiKey([FromBody] GenerateApiKeyRequest request)
    {
        // Get organization ID from claims
        var orgIdClaim = User.FindFirst("organizationId");
        if (orgIdClaim == null || !Guid.TryParse(orgIdClaim.Value, out var organizationId))
        {
            return Unauthorized(new { error = "Organization context required" });
        }

        // Override organization ID from token
        request.OrganizationId = organizationId;

        var result = await _apiKeyService.GenerateApiKeyAsync(request);

        return Ok(result);
    }

    /// <summary>
    /// Lists all API keys for the current user's organization.
    /// GET /apikeys
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> ListApiKeys([FromQuery] bool includeRevoked = false)
    {
        // Get organization ID from claims
        var orgIdClaim = User.FindFirst("organizationId");
        if (orgIdClaim == null || !Guid.TryParse(orgIdClaim.Value, out var organizationId))
        {
            return Unauthorized(new { error = "Organization context required" });
        }

        var keys = await _apiKeyService.ListApiKeysAsync(organizationId, includeRevoked);

        return Ok(keys);
    }

    /// <summary>
    /// Revokes an API key.
    /// DELETE /apikeys/{id}
    /// </summary>
    [HttpDelete("{id}")]
    public async Task<IActionResult> RevokeApiKey(Guid id, [FromBody] RevokeApiKeyRequest request)
    {
        // Get user ID from claims
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
        var userId = userIdClaim != null && Guid.TryParse(userIdClaim.Value, out var uid) ? uid : (Guid?)null;

        var success = await _apiKeyService.RevokeApiKeyAsync(id, request.Reason, userId);

        if (!success)
        {
            return NotFound(new { error = "API key not found" });
        }

        return Ok(new { message = "API key revoked successfully" });
    }

    /// <summary>
    /// Gets usage statistics for an API key.
    /// GET /apikeys/{id}/usage
    /// </summary>
    [HttpGet("{id}/usage")]
    public async Task<IActionResult> GetUsageStatistics(
        Guid id,
        [FromQuery] DateTime? from = null,
        [FromQuery] DateTime? to = null)
    {
        var fromDate = from ?? DateTime.UtcNow.AddDays(-30);
        var toDate = to ?? DateTime.UtcNow;

        var usage = await _apiKeyService.GetApiKeyUsageAsync(id, fromDate, toDate);

        return Ok(usage);
    }
}

/// <summary>
/// Request model for revoking an API key.
/// </summary>
public class RevokeApiKeyRequest
{
    /// <summary>
    /// Gets or sets the reason for revocation.
    /// </summary>
    public required string Reason { get; set; }
}
