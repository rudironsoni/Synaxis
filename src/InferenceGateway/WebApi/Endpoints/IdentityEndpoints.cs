using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Synaxis.InferenceGateway.Application.Identity;
using Synaxis.InferenceGateway.Application.Identity.Models;

namespace Synaxis.InferenceGateway.WebApi.Endpoints;

/// <summary>
/// API endpoints for identity management (authentication, registration, user management).
/// </summary>
[ApiController]
[Route("identity")]
public class IdentityEndpoints : ControllerBase
{
    private readonly IIdentityService _identityService;

    /// <summary>
    /// Initializes a new instance of the <see cref="IdentityEndpoints"/> class.
    /// </summary>
    public IdentityEndpoints(IIdentityService identityService)
    {
        _identityService = identityService;
    }

    /// <summary>
    /// Registers a new user and organization.
    /// POST /identity/register
    /// </summary>
    [HttpPost("register")]
    [AllowAnonymous]
    public async Task<IActionResult> Register([FromBody] RegisterRequest request)
    {
        var result = await _identityService.RegisterOrganizationAsync(request);

        if (!result.Success)
        {
            return BadRequest(new { errors = result.Errors });
        }

        return Ok(new
        {
            userId = result.UserId,
            organizationId = result.OrganizationId,
            message = "Registration successful"
        });
    }

    /// <summary>
    /// Logs in a user and returns JWT tokens.
    /// POST /identity/login
    /// </summary>
    [HttpPost("login")]
    [AllowAnonymous]
    public async Task<IActionResult> Login([FromBody] LoginRequest request)
    {
        var result = await _identityService.LoginAsync(request);

        if (!result.Success)
        {
            return Unauthorized(new { error = result.ErrorMessage });
        }

        return Ok(result);
    }

    /// <summary>
    /// Refreshes an access token using a refresh token.
    /// POST /identity/refresh
    /// </summary>
    [HttpPost("refresh")]
    [AllowAnonymous]
    public async Task<IActionResult> RefreshToken([FromBody] RefreshTokenRequest request)
    {
        var result = await _identityService.RefreshTokenAsync(request.RefreshToken);

        if (!result.Success)
        {
            return Unauthorized(new { error = result.ErrorMessage });
        }

        return Ok(result);
    }

    /// <summary>
    /// Gets information about the current user.
    /// GET /identity/me
    /// </summary>
    [HttpGet("me")]
    [Authorize]
    public async Task<IActionResult> GetCurrentUser()
    {
        var userIdClaim = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier);
        if (userIdClaim == null || !Guid.TryParse(userIdClaim.Value, out var userId))
        {
            return Unauthorized();
        }

        var userInfo = await _identityService.GetUserInfoAsync(userId);
        if (userInfo == null)
        {
            return NotFound();
        }

        return Ok(userInfo);
    }

    /// <summary>
    /// Gets all organizations for the current user.
    /// GET /identity/organizations
    /// </summary>
    [HttpGet("organizations")]
    [Authorize]
    public async Task<IActionResult> GetOrganizations()
    {
        var userIdClaim = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier);
        if (userIdClaim == null || !Guid.TryParse(userIdClaim.Value, out var userId))
        {
            return Unauthorized();
        }

        var userInfo = await _identityService.GetUserInfoAsync(userId);
        if (userInfo == null)
        {
            return NotFound();
        }

        return Ok(userInfo.Organizations);
    }

    /// <summary>
    /// Switches the current user's active organization.
    /// POST /identity/organizations/{id}/switch
    /// </summary>
    [HttpPost("organizations/{id}/switch")]
    [Authorize]
    public async Task<IActionResult> SwitchOrganization(Guid id)
    {
        // This would require regenerating the JWT with the new organization context
        // For now, return a placeholder response
        return Ok(new { message = "Organization switch requires new login", organizationId = id });
    }
}

/// <summary>
/// Request model for refresh token.
/// </summary>
public class RefreshTokenRequest
{
    /// <summary>
    /// Gets or sets the refresh token.
    /// </summary>
    public required string RefreshToken { get; set; }
}
