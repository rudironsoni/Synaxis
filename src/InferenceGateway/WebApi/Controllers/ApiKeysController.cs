// <copyright file="ApiKeysController.cs" company="Synaxis">
// Copyright (c) Synaxis. All rights reserved.
// </copyright>

namespace Synaxis.InferenceGateway.WebApi.Controllers
{
    using System.Security.Claims;
    using Microsoft.AspNetCore.Authorization;
    using Microsoft.AspNetCore.Cors;
    using Microsoft.AspNetCore.Mvc;
    using Microsoft.EntityFrameworkCore;
    using Synaxis.InferenceGateway.Application.ControlPlane.Entities;
    using Synaxis.InferenceGateway.Application.Security;
    using Synaxis.InferenceGateway.Infrastructure.ControlPlane;

    /// <summary>
    /// Controller for managing API keys.
    /// </summary>
    [ApiController]
    [Route("projects/{projectId}/keys")]
    [Authorize]
    [EnableCors("WebApp")]
    public class ApiKeysController : ControllerBase
    {
        private readonly ControlPlaneDbContext dbContext;
        private readonly IApiKeyService apiKeyService;
        private readonly IAuditService auditService;

        /// <summary>
        /// Initializes a new instance of the <see cref="ApiKeysController"/> class.
        /// </summary>
        /// <param name="dbContext">The database context.</param>
        /// <param name="apiKeyService">The API key service.</param>
        /// <param name="auditService">The audit service.</param>
        public ApiKeysController(ControlPlaneDbContext dbContext, IApiKeyService apiKeyService, IAuditService auditService)
        {
            this.dbContext = dbContext;
            this.apiKeyService = apiKeyService;
            this.auditService = auditService;
        }

        /// <summary>
        /// Creates a new API key.
        /// </summary>
        /// <param name="projectId">The project ID.</param>
        /// <param name="request">The create key request.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>The created API key.</returns>
        [HttpPost]
        public async Task<IActionResult> CreateKey(Guid projectId, [FromBody] CreateKeyRequest request, CancellationToken cancellationToken)
        {
            var userId = Guid.Parse(this.User.FindFirstValue(ClaimTypes.NameIdentifier) ?? this.User.FindFirstValue("sub") !);
            var tenantId = Guid.Parse(this.User.FindFirstValue("tenantId") !);

            var project = await this.dbContext.Projects
                .FirstOrDefaultAsync(p => p.Id == projectId && p.TenantId == tenantId, cancellationToken)
                .ConfigureAwait(false);

            if (project == null)
            {
                return this.NotFound("Project not found");
            }

            var rawKey = this.apiKeyService.GenerateKey();
            var hash = this.apiKeyService.HashKey(rawKey);

            var apiKey = new ApiKey
            {
                Id = Guid.NewGuid(),
                ProjectId = projectId,
                UserId = userId,
                Name = request.Name,
                KeyHash = hash,
                Status = ApiKeyStatus.Active,
                CreatedAt = DateTimeOffset.UtcNow,
            };

            this.dbContext.ApiKeys.Add(apiKey);
            await this.dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

            await this.auditService.LogAsync(tenantId, userId, "CreateApiKey", new { ApiKeyId = apiKey.Id, ProjectId = projectId }, cancellationToken).ConfigureAwait(false);

            return this.Ok(new { Id = apiKey.Id, Key = rawKey, Name = apiKey.Name });
        }

        /// <summary>
        /// Revokes an API key.
        /// </summary>
        /// <param name="projectId">The project ID.</param>
        /// <param name="keyId">The API key ID.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>No content.</returns>
        [HttpDelete("{keyId}")]
        public async Task<IActionResult> RevokeKey(Guid projectId, Guid keyId, CancellationToken cancellationToken)
        {
            var userId = Guid.Parse(this.User.FindFirstValue(ClaimTypes.NameIdentifier) ?? this.User.FindFirstValue("sub") !);
            var tenantId = Guid.Parse(this.User.FindFirstValue("tenantId") !);

            var apiKey = await this.dbContext.ApiKeys
                .Include(k => k.Project)
                .FirstOrDefaultAsync(k => k.Id == keyId && k.ProjectId == projectId && k.Project!.TenantId == tenantId, cancellationToken)
                .ConfigureAwait(false);

            if (apiKey == null)
            {
                return this.NotFound("API Key not found");
            }

            apiKey.Status = ApiKeyStatus.Revoked;
            await this.dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

            await this.auditService.LogAsync(tenantId, userId, "RevokeApiKey", new { ApiKeyId = keyId }, cancellationToken).ConfigureAwait(false);

            return this.NoContent();
        }
    }

    /// <summary>
    /// Request to create a new API key.
    /// </summary>
    public class CreateKeyRequest
    {
        /// <summary>
        /// Gets or sets the name of the API key.
        /// </summary>
        public string Name { get; set; } = string.Empty;
    }
}
