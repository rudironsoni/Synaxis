// <copyright file="VirtualKeysController.cs" company="Synaxis">
// Copyright (c) Synaxis. All rights reserved.
// </copyright>

namespace Synaxis.InferenceGateway.WebApi.Controllers
{
    using System;
    using System.Collections.Generic;
    using System.ComponentModel.DataAnnotations;
    using System.Linq;
    using System.Security.Claims;
    using System.Security.Cryptography;
    using System.Text;
    using System.Text.Json.Serialization;
    using System.Threading;
    using System.Threading.Tasks;
    using Microsoft.AspNetCore.Authorization;
    using Microsoft.AspNetCore.Cors;
    using Microsoft.AspNetCore.Mvc;
    using Microsoft.EntityFrameworkCore;
    using Synaxis.Core.Models;
    using Synaxis.Infrastructure.Data;

    /// <summary>
    /// Controller for managing Virtual Keys (API Keys with budget and rate limiting).
    /// </summary>
    [ApiController]
    [Route("api/v1/organizations/{orgId}/api-keys")]
    [Authorize]
    [EnableCors("WebApp")]
    public class VirtualKeysController : ControllerBase
    {
        private readonly SynaxisDbContext _dbContext;

        /// <summary>
        /// Initializes a new instance of the <see cref="VirtualKeysController"/> class.
        /// </summary>
        /// <param name="dbContext">The Synaxis database context.</param>
        public VirtualKeysController(SynaxisDbContext dbContext)
        {
            this._dbContext = dbContext;
        }

        /// <summary>
        /// Creates a new API key.
        /// </summary>
        /// <param name="orgId">The organization ID.</param>
        /// <param name="request">The create API key request.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>The created API key with plain text key (shown only once).</returns>
#pragma warning disable MA0051 // Method is too long - complex validation and key generation logic
        [HttpPost]
        public async Task<IActionResult> CreateApiKey(
            Guid orgId,
            [FromBody] CreateApiKeyRequest request,
            CancellationToken cancellationToken)
        {
            var userId = this.GetUserId();

            var validationResult = await this.ValidateCreateRequestAsync(orgId, request, cancellationToken).ConfigureAwait(false);
            if (validationResult != null)
            {
                return validationResult;
            }

            var hasPermission = await this.CheckTeamAdminOrOrgAdminAsync(userId, orgId, request.TeamId, cancellationToken).ConfigureAwait(false);
            if (!hasPermission)
            {
                return this.Forbid();
            }

            var (plainKey, keyHash) = GenerateApiKey();

            var user = await this._dbContext.Users.FindAsync(new object[] { userId }, cancellationToken).ConfigureAwait(false);
            var org = await this._dbContext.Organizations.FindAsync(new object[] { orgId }, cancellationToken).ConfigureAwait(false);
            var userRegion = user?.DataResidencyRegion ?? org?.PrimaryRegion ?? "us-east-1";

            var virtualKey = new VirtualKey
            {
                Id = Guid.NewGuid(),
                OrganizationId = orgId,
                TeamId = request.TeamId,
                CreatedBy = userId,
                Name = request.Name,
                Description = request.Description ?? string.Empty,
                KeyHash = keyHash,
                MaxBudget = request.MaxBudget,
                CurrentSpend = 0.00m,
                RpmLimit = request.RpmLimit,
                TpmLimit = request.TpmLimit,
                AllowedModels = request.AllowedModels?.ToList() ?? new List<string>(),
                BlockedModels = new List<string>(),
                ExpiresAt = request.ExpiresAt,
                Tags = request.Tags?.ToList() ?? new List<string>(),
                Metadata = new Dictionary<string, object>(),
                UserRegion = userRegion,
                IsActive = true,
                IsRevoked = false,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
            };

            this._dbContext.VirtualKeys.Add(virtualKey);
            await this._dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

            var response = new ApiKeyResponse
            {
                Id = virtualKey.Id,
                Name = virtualKey.Name,
                Description = virtualKey.Description,
                TeamId = virtualKey.TeamId,
                MaxBudget = virtualKey.MaxBudget,
                CurrentSpend = virtualKey.CurrentSpend,
                RpmLimit = virtualKey.RpmLimit,
                TpmLimit = virtualKey.TpmLimit,
                AllowedModels = virtualKey.AllowedModels,
                ExpiresAt = virtualKey.ExpiresAt,
                Tags = virtualKey.Tags,
                IsActive = virtualKey.IsActive,
                IsRevoked = virtualKey.IsRevoked,
                CreatedAt = virtualKey.CreatedAt,
                UpdatedAt = virtualKey.UpdatedAt,
                Key = plainKey,
            };

            return this.CreatedAtAction(nameof(this.GetApiKey), new { orgId, keyId = virtualKey.Id }, response);
#pragma warning restore MA0051
        }

        /// <summary>
        /// Lists API keys in an organization.
        /// </summary>
        /// <param name="orgId">The organization ID.</param>
        /// <param name="teamId">Optional team ID filter.</param>
        /// <param name="page">The page number.</param>
        /// <param name="pageSize">The page size.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>A paginated list of API keys.</returns>
        [HttpGet]
        public async Task<IActionResult> ListApiKeys(
            Guid orgId,
            [FromQuery] Guid? teamId,
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 20,
            CancellationToken cancellationToken = default)
        {
            var userId = this.GetUserId();

            var isMember = await this._dbContext.TeamMemberships
                .AnyAsync(tm => tm.OrganizationId == orgId && tm.UserId == userId, cancellationToken)
                .ConfigureAwait(false);

            if (!isMember)
            {
                return this.Forbid();
            }

            var query = this._dbContext.VirtualKeys
                .Where(k => k.OrganizationId == orgId);

            if (teamId.HasValue)
            {
                query = query.Where(k => k.TeamId == teamId.Value);
            }

            var totalCount = await query.CountAsync(cancellationToken).ConfigureAwait(false);

            var keys = await query
                .OrderByDescending(k => k.CreatedAt)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(k => new ApiKeySummaryResponse
                {
                    Id = k.Id,
                    Name = k.Name,
                    Description = k.Description,
                    TeamId = k.TeamId,
                    MaxBudget = k.MaxBudget,
                    CurrentSpend = k.CurrentSpend,
                    IsActive = k.IsActive,
                    IsRevoked = k.IsRevoked,
                    ExpiresAt = k.ExpiresAt,
                    CreatedAt = k.CreatedAt,
                    UpdatedAt = k.UpdatedAt,
                })
                .ToListAsync(cancellationToken)
                .ConfigureAwait(false);

            return this.Ok(new
            {
                items = keys,
                page,
                pageSize,
                totalCount,
                totalPages = (int)Math.Ceiling((double)totalCount / pageSize),
            });
        }

        /// <summary>
        /// Gets details of a specific API key.
        /// </summary>
        /// <param name="orgId">The organization ID.</param>
        /// <param name="keyId">The API key ID.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>The API key details.</returns>
        [HttpGet("{keyId}")]
        public async Task<IActionResult> GetApiKey(
            Guid orgId,
            Guid keyId,
            CancellationToken cancellationToken)
        {
            var userId = this.GetUserId();

            var isMember = await this._dbContext.TeamMemberships
                .AnyAsync(tm => tm.OrganizationId == orgId && tm.UserId == userId, cancellationToken)
                .ConfigureAwait(false);

            if (!isMember)
            {
                return this.Forbid();
            }

            var key = await this._dbContext.VirtualKeys
                .FirstOrDefaultAsync(k => k.Id == keyId && k.OrganizationId == orgId, cancellationToken)
                .ConfigureAwait(false);

            if (key == null)
            {
                return this.NotFound("API key not found");
            }

            var response = new ApiKeyDetailsResponse
            {
                Id = key.Id,
                Name = key.Name,
                Description = key.Description,
                TeamId = key.TeamId,
                MaxBudget = key.MaxBudget,
                CurrentSpend = key.CurrentSpend,
                RpmLimit = key.RpmLimit,
                TpmLimit = key.TpmLimit,
                AllowedModels = key.AllowedModels,
                BlockedModels = key.BlockedModels,
                ExpiresAt = key.ExpiresAt,
                Tags = key.Tags,
                IsActive = key.IsActive,
                IsRevoked = key.IsRevoked,
                RevokedAt = key.RevokedAt,
                RevokedReason = key.RevokedReason,
                CreatedAt = key.CreatedAt,
                UpdatedAt = key.UpdatedAt,
            };

            return this.Ok(response);
        }

        /// <summary>
        /// Updates an API key.
        /// </summary>
        /// <param name="orgId">The organization ID.</param>
        /// <param name="keyId">The API key ID.</param>
        /// <param name="request">The update request.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>The updated API key details.</returns>
        [HttpPut("{keyId}")]
        public async Task<IActionResult> UpdateApiKey(
            Guid orgId,
            Guid keyId,
            [FromBody] UpdateApiKeyRequest request,
            CancellationToken cancellationToken)
        {
            var userId = this.GetUserId();

            var key = await this._dbContext.VirtualKeys
                .FirstOrDefaultAsync(k => k.Id == keyId && k.OrganizationId == orgId, cancellationToken)
                .ConfigureAwait(false);

            if (key == null)
            {
                return this.NotFound("API key not found");
            }

            var hasPermission = await this.CheckTeamAdminOrOrgAdminAsync(userId, orgId, key.TeamId, cancellationToken).ConfigureAwait(false);
            if (!hasPermission)
            {
                return this.Forbid();
            }

            var validationResult = this.ValidateUpdateRequest(request);
            if (validationResult != null)
            {
                return validationResult;
            }

            ApplyUpdates(key, request);

            await this._dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

            var response = new ApiKeyDetailsResponse
            {
                Id = key.Id,
                Name = key.Name,
                Description = key.Description,
                TeamId = key.TeamId,
                MaxBudget = key.MaxBudget,
                CurrentSpend = key.CurrentSpend,
                RpmLimit = key.RpmLimit,
                TpmLimit = key.TpmLimit,
                AllowedModels = key.AllowedModels,
                BlockedModels = key.BlockedModels,
                ExpiresAt = key.ExpiresAt,
                Tags = key.Tags,
                IsActive = key.IsActive,
                IsRevoked = key.IsRevoked,
                RevokedAt = key.RevokedAt,
                RevokedReason = key.RevokedReason,
                CreatedAt = key.CreatedAt,
                UpdatedAt = key.UpdatedAt,
            };

            return this.Ok(response);
        }

        /// <summary>
        /// Revokes an API key.
        /// </summary>
        /// <param name="orgId">The organization ID.</param>
        /// <param name="keyId">The API key ID.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>No content.</returns>
        [HttpDelete("{keyId}")]
        public async Task<IActionResult> RevokeApiKey(
            Guid orgId,
            Guid keyId,
            CancellationToken cancellationToken)
        {
            var userId = this.GetUserId();

            var key = await this._dbContext.VirtualKeys
                .FirstOrDefaultAsync(k => k.Id == keyId && k.OrganizationId == orgId, cancellationToken)
                .ConfigureAwait(false);

            if (key == null)
            {
                return this.NotFound("API key not found");
            }

            var hasPermission = await this.CheckTeamAdminOrOrgAdminOrCreatorAsync(userId, orgId, key.TeamId, key.CreatedBy, cancellationToken).ConfigureAwait(false);
            if (!hasPermission)
            {
                return this.Forbid();
            }

            key.IsRevoked = true;
            key.RevokedAt = DateTime.UtcNow;
            key.UpdatedAt = DateTime.UtcNow;

            await this._dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

            return this.NoContent();
        }

        /// <summary>
        /// Rotates an API key (generates new key hash).
        /// </summary>
        /// <param name="orgId">The organization ID.</param>
        /// <param name="keyId">The API key ID.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>The new API key (plain text, shown only once).</returns>
        [HttpPost("{keyId}/rotate")]
        public async Task<IActionResult> RotateApiKey(
            Guid orgId,
            Guid keyId,
            CancellationToken cancellationToken)
        {
            var userId = this.GetUserId();

            var key = await this._dbContext.VirtualKeys
                .FirstOrDefaultAsync(k => k.Id == keyId && k.OrganizationId == orgId, cancellationToken)
                .ConfigureAwait(false);

            if (key == null)
            {
                return this.NotFound("API key not found");
            }

            var hasPermission = await this.CheckTeamAdminOrOrgAdminOrCreatorAsync(userId, orgId, key.TeamId, key.CreatedBy, cancellationToken).ConfigureAwait(false);
            if (!hasPermission)
            {
                return this.Forbid();
            }

            var (plainKey, keyHash) = GenerateApiKey();

            key.KeyHash = keyHash;
            key.UpdatedAt = DateTime.UtcNow;

            await this._dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

            return this.Ok(new { id = key.Id, key = plainKey, name = key.Name });
        }

        /// <summary>
        /// Gets usage statistics for an API key.
        /// </summary>
        /// <param name="orgId">The organization ID.</param>
        /// <param name="keyId">The API key ID.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>Usage statistics.</returns>
        [HttpGet("{keyId}/usage")]
        public async Task<IActionResult> GetUsage(
            Guid orgId,
            Guid keyId,
            CancellationToken cancellationToken)
        {
            var userId = this.GetUserId();

            var isMember = await this._dbContext.TeamMemberships
                .AnyAsync(tm => tm.OrganizationId == orgId && tm.UserId == userId, cancellationToken)
                .ConfigureAwait(false);

            if (!isMember)
            {
                return this.Forbid();
            }

            var key = await this._dbContext.VirtualKeys
                .FirstOrDefaultAsync(k => k.Id == keyId && k.OrganizationId == orgId, cancellationToken)
                .ConfigureAwait(false);

            if (key == null)
            {
                return this.NotFound("API key not found");
            }

            var requestCount = await this._dbContext.Requests
                .CountAsync(r => r.VirtualKeyId == keyId, cancellationToken)
                .ConfigureAwait(false);

            var response = new ApiKeyUsageResponse
            {
                CurrentSpend = key.CurrentSpend,
                RemainingBudget = key.RemainingBudget,
                RequestCount = requestCount,
            };

            return this.Ok(response);
        }

        private static (string PlainKey, string KeyHash) GenerateApiKey()
        {
            var plainKey = "sk_" + Convert.ToBase64String(RandomNumberGenerator.GetBytes(32));
            var keyHash = Convert.ToBase64String(SHA256.HashData(Encoding.UTF8.GetBytes(plainKey)));
            return (plainKey, keyHash);
        }

        private Guid GetUserId()
        {
            var userIdClaim = this.User.FindFirstValue(ClaimTypes.NameIdentifier) ?? this.User.FindFirstValue("sub");
            return Guid.Parse(userIdClaim!);
        }

        private async Task<IActionResult?> ValidateCreateRequestAsync(Guid orgId, CreateApiKeyRequest request, CancellationToken cancellationToken)
        {
            if (string.IsNullOrWhiteSpace(request.Name))
            {
                return this.BadRequest("Name is required");
            }

            if (request.MaxBudget.HasValue && request.MaxBudget < 0)
            {
                return this.BadRequest("MaxBudget cannot be negative");
            }

            if (request.RpmLimit.HasValue && request.RpmLimit < 0)
            {
                return this.BadRequest("RpmLimit cannot be negative");
            }

            if (request.TpmLimit.HasValue && request.TpmLimit < 0)
            {
                return this.BadRequest("TpmLimit cannot be negative");
            }

            var teamExists = await this._dbContext.Teams
                .AnyAsync(t => t.Id == request.TeamId && t.OrganizationId == orgId, cancellationToken)
                .ConfigureAwait(false);

            if (!teamExists)
            {
                return this.NotFound("Team not found");
            }

            return null;
        }

        private IActionResult? ValidateUpdateRequest(UpdateApiKeyRequest request)
        {
            if (request.MaxBudget.HasValue && request.MaxBudget < 0)
            {
                return this.BadRequest("MaxBudget cannot be negative");
            }

            if (request.RpmLimit.HasValue && request.RpmLimit < 0)
            {
                return this.BadRequest("RpmLimit cannot be negative");
            }

            if (request.TpmLimit.HasValue && request.TpmLimit < 0)
            {
                return this.BadRequest("TpmLimit cannot be negative");
            }

            return null;
        }

        private static void ApplyUpdates(VirtualKey key, UpdateApiKeyRequest request)
        {
            if (!string.IsNullOrWhiteSpace(request.Name))
            {
                key.Name = request.Name;
            }

            if (request.Description != null)
            {
                key.Description = request.Description;
            }

            if (request.MaxBudget.HasValue)
            {
                key.MaxBudget = request.MaxBudget;
            }

            if (request.RpmLimit.HasValue)
            {
                key.RpmLimit = request.RpmLimit;
            }

            if (request.TpmLimit.HasValue)
            {
                key.TpmLimit = request.TpmLimit;
            }

            if (request.ExpiresAt.HasValue)
            {
                key.ExpiresAt = request.ExpiresAt;
            }

            if (request.AllowedModels != null)
            {
                key.AllowedModels = request.AllowedModels.ToList();
            }

            if (request.IsActive.HasValue)
            {
                key.IsActive = request.IsActive.Value;
            }

            key.UpdatedAt = DateTime.UtcNow;
        }

        private async Task<bool> CheckTeamAdminOrOrgAdminAsync(Guid userId, Guid orgId, Guid teamId, CancellationToken cancellationToken)
        {
            var membership = await this._dbContext.TeamMemberships
                .FirstOrDefaultAsync(tm => tm.UserId == userId && tm.TeamId == teamId && tm.OrganizationId == orgId, cancellationToken)
                .ConfigureAwait(false);

            if (membership != null && (string.Equals(membership.Role, "TeamAdmin", StringComparison.Ordinal) || string.Equals(membership.Role, "OrgAdmin", StringComparison.Ordinal)))
            {
                return true;
            }

            var user = await this._dbContext.Users
                .FirstOrDefaultAsync(u => u.Id == userId && u.OrganizationId == orgId, cancellationToken)
                .ConfigureAwait(false);

            var isOrgAdmin = user != null && (string.Equals(user.Role, "admin", StringComparison.OrdinalIgnoreCase) || string.Equals(user.Role, "owner", StringComparison.OrdinalIgnoreCase));

            return isOrgAdmin;
        }

        private async Task<bool> CheckTeamAdminOrOrgAdminOrCreatorAsync(Guid userId, Guid orgId, Guid teamId, Guid creatorId, CancellationToken cancellationToken)
        {
            if (userId == creatorId)
            {
                return true;
            }

            return await this.CheckTeamAdminOrOrgAdminAsync(userId, orgId, teamId, cancellationToken).ConfigureAwait(false);
        }
    }

    /// <summary>
    /// Request to create a new API key.
    /// </summary>
    public class CreateApiKeyRequest
    {
        /// <summary>
        /// Gets or sets the team ID.
        /// </summary>
        [Required]
        [JsonRequired]
        public Guid TeamId { get; set; }

        /// <summary>
        /// Gets or sets the key name.
        /// </summary>
        [Required]
        [StringLength(255)]
        public string Name { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the key description.
        /// </summary>
        public string? Description { get; set; }

        /// <summary>
        /// Gets or sets the maximum budget.
        /// </summary>
        public decimal? MaxBudget { get; set; }

        /// <summary>
        /// Gets or sets the requests per minute limit.
        /// </summary>
        public int? RpmLimit { get; set; }

        /// <summary>
        /// Gets or sets the tokens per minute limit.
        /// </summary>
        public int? TpmLimit { get; set; }

        /// <summary>
        /// Gets or sets the expiration date.
        /// </summary>
        public DateTime? ExpiresAt { get; set; }

        /// <summary>
        /// Gets or sets the allowed models.
        /// </summary>
        public IEnumerable<string>? AllowedModels { get; set; }

        /// <summary>
        /// Gets or sets the tags.
        /// </summary>
        public IEnumerable<string>? Tags { get; set; }
    }

    /// <summary>
    /// Request to update an API key.
    /// </summary>
    public class UpdateApiKeyRequest
    {
        /// <summary>
        /// Gets or sets the key name.
        /// </summary>
        public string? Name { get; set; }

        /// <summary>
        /// Gets or sets the key description.
        /// </summary>
        public string? Description { get; set; }

        /// <summary>
        /// Gets or sets the maximum budget.
        /// </summary>
        public decimal? MaxBudget { get; set; }

        /// <summary>
        /// Gets or sets the requests per minute limit.
        /// </summary>
        public int? RpmLimit { get; set; }

        /// <summary>
        /// Gets or sets the tokens per minute limit.
        /// </summary>
        public int? TpmLimit { get; set; }

        /// <summary>
        /// Gets or sets the expiration date.
        /// </summary>
        public DateTime? ExpiresAt { get; set; }

        /// <summary>
        /// Gets or sets the allowed models.
        /// </summary>
        public IEnumerable<string>? AllowedModels { get; set; }

        /// <summary>
        /// Gets or sets whether the key is active.
        /// </summary>
        public bool? IsActive { get; set; }
    }

    /// <summary>
    /// API key response with plain text key (only returned on create/rotate).
    /// </summary>
    public class ApiKeyResponse
    {
        /// <summary>
        /// Gets or sets the key ID.
        /// </summary>
        public Guid Id { get; set; }

        /// <summary>
        /// Gets or sets the key name.
        /// </summary>
        public string Name { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the key description.
        /// </summary>
        public string Description { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the team ID.
        /// </summary>
        public Guid TeamId { get; set; }

        /// <summary>
        /// Gets or sets the maximum budget.
        /// </summary>
        public decimal? MaxBudget { get; set; }

        /// <summary>
        /// Gets or sets the current spend.
        /// </summary>
        public decimal CurrentSpend { get; set; }

        /// <summary>
        /// Gets or sets the requests per minute limit.
        /// </summary>
        public int? RpmLimit { get; set; }

        /// <summary>
        /// Gets or sets the tokens per minute limit.
        /// </summary>
        public int? TpmLimit { get; set; }

        /// <summary>
        /// Gets or sets the allowed models.
        /// </summary>
        public IList<string> AllowedModels { get; set; } = new List<string>();

        /// <summary>
        /// Gets or sets the expiration date.
        /// </summary>
        public DateTime? ExpiresAt { get; set; }

        /// <summary>
        /// Gets or sets the tags.
        /// </summary>
        public IList<string> Tags { get; set; } = new List<string>();

        /// <summary>
        /// Gets or sets a value indicating whether gets or sets whether the key is active.
        /// </summary>
        public bool IsActive { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether gets or sets whether the key is revoked.
        /// </summary>
        public bool IsRevoked { get; set; }

        /// <summary>
        /// Gets or sets the creation timestamp.
        /// </summary>
        public DateTime CreatedAt { get; set; }

        /// <summary>
        /// Gets or sets the last update timestamp.
        /// </summary>
        public DateTime UpdatedAt { get; set; }

        /// <summary>
        /// Gets or sets the plain text API key (only returned on create/rotate).
        /// </summary>
        public string? Key { get; set; }
    }

    /// <summary>
    /// API key summary response for list endpoints.
    /// </summary>
    public class ApiKeySummaryResponse
    {
        /// <summary>
        /// Gets or sets the key ID.
        /// </summary>
        public Guid Id { get; set; }

        /// <summary>
        /// Gets or sets the key name.
        /// </summary>
        public string Name { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the key description.
        /// </summary>
        public string Description { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the team ID.
        /// </summary>
        public Guid TeamId { get; set; }

        /// <summary>
        /// Gets or sets the maximum budget.
        /// </summary>
        public decimal? MaxBudget { get; set; }

        /// <summary>
        /// Gets or sets the current spend.
        /// </summary>
        public decimal CurrentSpend { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether gets or sets whether the key is active.
        /// </summary>
        public bool IsActive { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether gets or sets whether the key is revoked.
        /// </summary>
        public bool IsRevoked { get; set; }

        /// <summary>
        /// Gets or sets the expiration date.
        /// </summary>
        public DateTime? ExpiresAt { get; set; }

        /// <summary>
        /// Gets or sets the creation timestamp.
        /// </summary>
        public DateTime CreatedAt { get; set; }

        /// <summary>
        /// Gets or sets the last update timestamp.
        /// </summary>
        public DateTime UpdatedAt { get; set; }
    }

    /// <summary>
    /// API key details response.
    /// </summary>
    public class ApiKeyDetailsResponse
    {
        /// <summary>
        /// Gets or sets the key ID.
        /// </summary>
        public Guid Id { get; set; }

        /// <summary>
        /// Gets or sets the key name.
        /// </summary>
        public string Name { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the key description.
        /// </summary>
        public string Description { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the team ID.
        /// </summary>
        public Guid TeamId { get; set; }

        /// <summary>
        /// Gets or sets the maximum budget.
        /// </summary>
        public decimal? MaxBudget { get; set; }

        /// <summary>
        /// Gets or sets the current spend.
        /// </summary>
        public decimal CurrentSpend { get; set; }

        /// <summary>
        /// Gets or sets the requests per minute limit.
        /// </summary>
        public int? RpmLimit { get; set; }

        /// <summary>
        /// Gets or sets the tokens per minute limit.
        /// </summary>
        public int? TpmLimit { get; set; }

        /// <summary>
        /// Gets or sets the allowed models.
        /// </summary>
        public IList<string> AllowedModels { get; set; } = new List<string>();

        /// <summary>
        /// Gets or sets the blocked models.
        /// </summary>
        public IList<string> BlockedModels { get; set; } = new List<string>();

        /// <summary>
        /// Gets or sets the expiration date.
        /// </summary>
        public DateTime? ExpiresAt { get; set; }

        /// <summary>
        /// Gets or sets the tags.
        /// </summary>
        public IList<string> Tags { get; set; } = new List<string>();

        /// <summary>
        /// Gets or sets a value indicating whether gets or sets whether the key is active.
        /// </summary>
        public bool IsActive { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether gets or sets whether the key is revoked.
        /// </summary>
        public bool IsRevoked { get; set; }

        /// <summary>
        /// Gets or sets the revocation timestamp.
        /// </summary>
        public DateTime? RevokedAt { get; set; }

        /// <summary>
        /// Gets or sets the revocation reason.
        /// </summary>
        public string? RevokedReason { get; set; }

        /// <summary>
        /// Gets or sets the creation timestamp.
        /// </summary>
        public DateTime CreatedAt { get; set; }

        /// <summary>
        /// Gets or sets the last update timestamp.
        /// </summary>
        public DateTime UpdatedAt { get; set; }
    }

    /// <summary>
    /// API key usage statistics response.
    /// </summary>
    public class ApiKeyUsageResponse
    {
        /// <summary>
        /// Gets or sets the current spend.
        /// </summary>
        public decimal CurrentSpend { get; set; }

        /// <summary>
        /// Gets or sets the remaining budget.
        /// </summary>
        public decimal? RemainingBudget { get; set; }

        /// <summary>
        /// Gets or sets the request count.
        /// </summary>
        public int RequestCount { get; set; }
    }
}
