// <copyright file="ApiKeyEndpoints.cs" company="Synaxis">
// Copyright (c) Synaxis. All rights reserved.
// </copyright>

namespace Synaxis.InferenceGateway.WebApi.Endpoints
{
    using System.Security.Claims;
    using Microsoft.AspNetCore.Authorization;
    using Microsoft.AspNetCore.Mvc;
    using Synaxis.InferenceGateway.Application.ApiKeys;
    using Synaxis.InferenceGateway.Application.ApiKeys.Models;

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
        /// <param name="apiKeyService">The API key service.</param>
        public ApiKeyEndpoints(IApiKeyService apiKeyService)
        {
            this._apiKeyService = apiKeyService;
        }

        /// <summary>
        /// Generates a new API key for the current user's organization.
        /// POST /apikeys
        /// </summary>
        /// <param name="request">The API key generation request.</param>
        /// <returns>The generated API key.</returns>
        [HttpPost]
        public async Task<IActionResult> GenerateApiKey([FromBody] GenerateApiKeyRequest request)
        {
            // Get organization ID from claims
            var orgIdClaim = this.User.FindFirst("organizationId");
            if (orgIdClaim == null || !Guid.TryParse(orgIdClaim.Value, out var organizationId))
            {
                return this.Unauthorized(new { error = "Organization context required" });
            }

            // Override organization ID from token
            request.OrganizationId = organizationId;

            var result = await this._apiKeyService.GenerateApiKeyAsync(request).ConfigureAwait(false);

            return this.Ok(result);
        }

        /// <summary>
        /// Lists all API keys for the current user's organization.
        /// GET /apikeys
        /// </summary>
        /// <param name="includeRevoked">Whether to include revoked keys.</param>
        /// <returns>The list of API keys.</returns>
        [HttpGet]
        public async Task<IActionResult> ListApiKeys([FromQuery] bool includeRevoked = false)
        {
            // Get organization ID from claims
            var orgIdClaim = this.User.FindFirst("organizationId");
            if (orgIdClaim == null || !Guid.TryParse(orgIdClaim.Value, out var organizationId))
            {
                return this.Unauthorized(new { error = "Organization context required" });
            }

            var keys = await this._apiKeyService.ListApiKeysAsync(organizationId, includeRevoked).ConfigureAwait(false);

            return this.Ok(keys);
        }

        /// <summary>
        /// Revokes an API key.
        /// DELETE /apikeys/{id}
        /// </summary>
        /// <param name="id">The API key ID.</param>
        /// <param name="request">The revocation request.</param>
        /// <returns>The result of the revocation.</returns>
        [HttpDelete("{id}")]
        public async Task<IActionResult> RevokeApiKey(Guid id, [FromBody] RevokeApiKeyRequest request)
        {
            // Get user ID from claims
            var userIdClaim = this.User.FindFirst(ClaimTypes.NameIdentifier);
            var userId = userIdClaim != null && Guid.TryParse(userIdClaim.Value, out var uid) ? uid : (Guid?)null;

            var success = await this._apiKeyService.RevokeApiKeyAsync(id, request.Reason, userId).ConfigureAwait(false);

            if (!success)
            {
                return this.NotFound(new { error = "API key not found" });
            }

            return this.Ok(new { message = "API key revoked successfully" });
        }

        /// <summary>
        /// Gets usage statistics for an API key.
        /// GET /apikeys/{id}/usage
        /// </summary>
        /// <param name="id">The API key ID.</param>
        /// <param name="from">Start date for statistics.</param>
        /// <param name="to">End date for statistics.</param>
        /// <returns>The usage statistics.</returns>
        [HttpGet("{id}/usage")]
        public async Task<IActionResult> GetUsageStatistics(
            Guid id,
            [FromQuery] DateTime? from = null,
            [FromQuery] DateTime? to = null)
        {
            var fromDate = from ?? DateTime.UtcNow.AddDays(-30);
            var toDate = to ?? DateTime.UtcNow;

            var usage = await this._apiKeyService.GetApiKeyUsageAsync(id, fromDate, toDate).ConfigureAwait(false);

            return this.Ok(usage);
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
}
