// <copyright file="IdentityEndpoints.cs" company="Synaxis">
// Copyright (c) Synaxis. All rights reserved.
// </copyright>

namespace Synaxis.InferenceGateway.WebApi.Endpoints
{
    using Microsoft.AspNetCore.Authorization;
    using Microsoft.AspNetCore.Mvc;
    using Synaxis.InferenceGateway.Application.Identity;
    using Synaxis.InferenceGateway.Application.Identity.Models;

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
        /// <param name="identityService">The identity service.</param>
        public IdentityEndpoints(IIdentityService identityService)
        {
            this._identityService = identityService;
        }

        /// <summary>
        /// Registers a new user and organization.
        /// POST /identity/register.
        /// </summary>
        /// <param name="request">The registration request.</param>
        /// <returns>The registration result.</returns>
        [HttpPost("register")]
        [AllowAnonymous]
        public async Task<IActionResult> Register([FromBody] RegisterRequest request)
        {
            var result = await this._identityService.RegisterOrganizationAsync(request).ConfigureAwait(false);

            if (!result.Success)
            {
                return this.BadRequest(new { errors = result.Errors });
            }

            return this.Ok(new
            {
                userId = result.UserId,
                organizationId = result.OrganizationId,
                message = "Registration successful",
            });
        }

        /// <summary>
        /// Logs in a user and returns JWT tokens.
        /// POST /identity/login.
        /// </summary>
        /// <param name="request">The login request.</param>
        /// <returns>The login result with JWT tokens.</returns>
        [HttpPost("login")]
        [AllowAnonymous]
        public async Task<IActionResult> Login([FromBody] LoginRequest request)
        {
            var result = await this._identityService.LoginAsync(request).ConfigureAwait(false);

            if (!result.Success)
            {
                return this.Unauthorized(new { error = result.ErrorMessage });
            }

            return this.Ok(result);
        }

        /// <summary>
        /// Refreshes an access token using a refresh token.
        /// POST /identity/refresh.
        /// </summary>
        /// <param name="request">The refresh token request.</param>
        /// <returns>The new access token.</returns>
        [HttpPost("refresh")]
        [AllowAnonymous]
        public async Task<IActionResult> RefreshToken([FromBody] RefreshTokenRequest request)
        {
            var result = await this._identityService.RefreshTokenAsync(request.RefreshToken).ConfigureAwait(false);

            if (!result.Success)
            {
                return this.Unauthorized(new { error = result.ErrorMessage });
            }

            return this.Ok(result);
        }

        /// <summary>
        /// Gets information about the current user.
        /// GET /identity/me.
        /// </summary>
        /// <returns>The current user's information.</returns>
        [HttpGet("me")]
        [Authorize]
        public async Task<IActionResult> GetCurrentUser()
        {
            var userIdClaim = this.User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier);
            if (userIdClaim == null || !Guid.TryParse(userIdClaim.Value, out var userId))
            {
                return this.Unauthorized();
            }

            var userInfo = await this._identityService.GetUserInfoAsync(userId).ConfigureAwait(false);
            if (userInfo == null)
            {
                return this.NotFound();
            }

            return this.Ok(userInfo);
        }

        /// <summary>
        /// Gets all organizations for the current user.
        /// GET /identity/organizations.
        /// </summary>
        /// <returns>The list of organizations.</returns>
        [HttpGet("organizations")]
        [Authorize]
        public async Task<IActionResult> GetOrganizations()
        {
            var userIdClaim = this.User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier);
            if (userIdClaim == null || !Guid.TryParse(userIdClaim.Value, out var userId))
            {
                return this.Unauthorized();
            }

            var userInfo = await this._identityService.GetUserInfoAsync(userId).ConfigureAwait(false);
            if (userInfo == null)
            {
                return this.NotFound();
            }

            return this.Ok(userInfo.Organizations);
        }

        /// <summary>
        /// Switches the current user's active organization.
        /// POST /identity/organizations/{id}/switch.
        /// </summary>
        /// <param name="id">The organization ID to switch to.</param>
        /// <returns>The switch result.</returns>
        [HttpPost("organizations/{id}/switch")]
        [Authorize]
        public async Task<IActionResult> SwitchOrganization(Guid id)
        {
            // This would require regenerating the JWT with the new organization context
            // For now, return a placeholder response
            await Task.CompletedTask.ConfigureAwait(false);
            return this.Ok(new { message = "Organization switch requires new login", organizationId = id });
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
}
