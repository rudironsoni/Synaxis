// <copyright file="OrganizationApiKeysController.cs" company="Synaxis">
// Copyright (c) Synaxis. All rights reserved.
// </copyright>

namespace Synaxis.InferenceGateway.WebApi.Controllers.Organizations
{
    using System;
    using System.Globalization;
    using System.Linq;
    using System.Security.Claims;
    using System.Security.Cryptography;
    using System.Text;
    using System.Threading;
    using System.Threading.Tasks;
    using Microsoft.AspNetCore.Authorization;
    using Microsoft.AspNetCore.Cors;
    using Microsoft.AspNetCore.Mvc;
    using Microsoft.EntityFrameworkCore;
    using Synaxis.Core.Models;
    using Synaxis.Infrastructure.Data;

    /// <summary>
    /// Controller for managing organization API keys.
    /// </summary>
    [ApiController]
    [Route("api/v1/organizations/{organizationId}/api-keys")]
    [Authorize]
    [EnableCors("WebApp")]
    public class OrganizationApiKeysController : ControllerBase
    {
        private readonly SynaxisDbContext _synaxisDbContext;

        /// <summary>
        /// Initializes a new instance of the <see cref="OrganizationApiKeysController"/> class.
        /// </summary>
        /// <param name="synaxisDbContext">The Synaxis database context.</param>
        public OrganizationApiKeysController(SynaxisDbContext synaxisDbContext)
        {
            this._synaxisDbContext = synaxisDbContext;
        }

        /// <summary>
        /// Creates a new API key for an organization.
        /// </summary>
        /// <param name="organizationId">The organization ID.</param>
        /// <param name="request">The create API key request.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>The created API key with the actual key value.</returns>
        [HttpPost]
        public async Task<IActionResult> CreateApiKey(
            Guid organizationId,
            [FromBody] CreateOrganizationApiKeyRequest request,
            CancellationToken cancellationToken)
        {
            var userId = this.GetCurrentUserId();

            var organization = await this._synaxisDbContext.Organizations
                .FirstOrDefaultAsync(o => o.Id == organizationId, cancellationToken)
                .ConfigureAwait(false);

            if (organization == null)
            {
                return this.NotFound("Organization not found");
            }

            if (!await this.IsOrgAdminAsync(userId, organizationId, cancellationToken).ConfigureAwait(false))
            {
                return this.Forbid();
            }

            var (apiKey, keyValue) = this.CreateApiKeyEntity(organizationId, userId, request);
            this._synaxisDbContext.OrganizationApiKeys.Add(apiKey);
            await this._synaxisDbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

            var response = new CreateOrganizationApiKeyResponse
            {
                Id = apiKey.Id,
                Name = apiKey.Name,
                Key = keyValue,
                KeyPrefix = apiKey.KeyPrefix,
                Permissions = apiKey.Permissions,
                ExpiresAt = apiKey.ExpiresAt,
                IsActive = apiKey.IsActive,
                CreatedAt = apiKey.CreatedAt,
            };

            return this.CreatedAtAction(
                nameof(this.GetApiKey),
                new { organizationId = organizationId, keyId = apiKey.Id },
                response);
        }

        /// <summary>
        /// Lists API keys for an organization.
        /// </summary>
        /// <param name="organizationId">The organization ID.</param>
        /// <param name="page">The page number (1-based).</param>
        /// <param name="pageSize">The page size.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>A paginated list of API keys.</returns>
        [HttpGet]
        public async Task<IActionResult> ListApiKeys(
            Guid organizationId,
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 10,
            CancellationToken cancellationToken = default)
        {
            var userId = this.GetCurrentUserId();

            var organization = await this._synaxisDbContext.Organizations
                .FirstOrDefaultAsync(o => o.Id == organizationId, cancellationToken)
                .ConfigureAwait(false);

            if (organization == null)
            {
                return this.NotFound("Organization not found");
            }

            var isMember = await this._synaxisDbContext.TeamMemberships
                .AnyAsync(tm => tm.OrganizationId == organizationId && tm.UserId == userId, cancellationToken)
                .ConfigureAwait(false);

            if (!isMember)
            {
                return this.Forbid();
            }

            var query = this._synaxisDbContext.OrganizationApiKeys
                .Where(k => k.OrganizationId == organizationId)
                .Include(k => k.Creator);

            var totalCount = await query.CountAsync(cancellationToken).ConfigureAwait(false);

            var apiKeys = await query
                .OrderByDescending(k => k.CreatedAt)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(k => MapToOrganizationApiKeyResponse(k))
                .ToListAsync(cancellationToken)
                .ConfigureAwait(false);

            return this.Ok(new
            {
                items = apiKeys,
                page,
                pageSize,
                totalCount,
                totalPages = (int)Math.Ceiling((double)totalCount / pageSize),
            });
        }

        /// <summary>
        /// Gets an API key by ID.
        /// </summary>
        /// <param name="organizationId">The organization ID.</param>
        /// <param name="keyId">The API key ID.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>The API key details.</returns>
        [HttpGet("{keyId}")]
        public async Task<IActionResult> GetApiKey(
            Guid organizationId,
            Guid keyId,
            CancellationToken cancellationToken)
        {
            var userId = this.GetCurrentUserId();

            var organization = await this._synaxisDbContext.Organizations
                .FirstOrDefaultAsync(o => o.Id == organizationId, cancellationToken)
                .ConfigureAwait(false);

            if (organization == null)
            {
                return this.NotFound("Organization not found");
            }

            var isMember = await this._synaxisDbContext.TeamMemberships
                .AnyAsync(tm => tm.OrganizationId == organizationId && tm.UserId == userId, cancellationToken)
                .ConfigureAwait(false);

            if (!isMember)
            {
                return this.Forbid();
            }

            var apiKey = await this._synaxisDbContext.OrganizationApiKeys
                .Include(k => k.Creator)
                .FirstOrDefaultAsync(k => k.Id == keyId && k.OrganizationId == organizationId, cancellationToken)
                .ConfigureAwait(false);

            if (apiKey == null)
            {
                return this.NotFound("API key not found");
            }

            var response = MapToOrganizationApiKeyResponse(apiKey);
            return this.Ok(response);
        }

        /// <summary>
        /// Updates an API key.
        /// </summary>
        /// <param name="organizationId">The organization ID.</param>
        /// <param name="keyId">The API key ID.</param>
        /// <param name="request">The update API key request.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>The updated API key.</returns>
        [HttpPut("{keyId}")]
        public async Task<IActionResult> UpdateApiKey(
            Guid organizationId,
            Guid keyId,
            [FromBody] UpdateOrganizationApiKeyRequest request,
            CancellationToken cancellationToken)
        {
            var userId = this.GetCurrentUserId();

            var organization = await this._synaxisDbContext.Organizations
                .FirstOrDefaultAsync(o => o.Id == organizationId, cancellationToken)
                .ConfigureAwait(false);

            if (organization == null)
            {
                return this.NotFound("Organization not found");
            }

            if (!await this.IsOrgAdminAsync(userId, organizationId, cancellationToken).ConfigureAwait(false))
            {
                return this.Forbid();
            }

            var apiKey = await this._synaxisDbContext.OrganizationApiKeys
                .FirstOrDefaultAsync(k => k.Id == keyId && k.OrganizationId == organizationId, cancellationToken)
                .ConfigureAwait(false);

            if (apiKey == null)
            {
                return this.NotFound("API key not found");
            }

            if (!string.IsNullOrWhiteSpace(request.Name))
            {
                apiKey.Name = request.Name;
            }

            if (request.Permissions != null)
            {
                apiKey.Permissions = request.Permissions;
            }

            if (request.Revoke.HasValue && request.Revoke.Value)
            {
                apiKey.IsActive = false;
                apiKey.RevokedAt = DateTime.UtcNow;
                apiKey.RevokedReason = request.RevokedReason ?? "Manually revoked";
            }

            apiKey.UpdatedAt = DateTime.UtcNow;
            await this._synaxisDbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

            var response = MapToOrganizationApiKeyResponse(apiKey);
            return this.Ok(response);
        }

        /// <summary>
        /// Deletes an API key.
        /// </summary>
        /// <param name="organizationId">The organization ID.</param>
        /// <param name="keyId">The API key ID.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>No content.</returns>
        [HttpDelete("{keyId}")]
        public async Task<IActionResult> DeleteApiKey(
            Guid organizationId,
            Guid keyId,
            CancellationToken cancellationToken)
        {
            var userId = this.GetCurrentUserId();

            var organization = await this._synaxisDbContext.Organizations
                .FirstOrDefaultAsync(o => o.Id == organizationId, cancellationToken)
                .ConfigureAwait(false);

            if (organization == null)
            {
                return this.NotFound("Organization not found");
            }

            if (!await this.IsOrgAdminAsync(userId, organizationId, cancellationToken).ConfigureAwait(false))
            {
                return this.Forbid();
            }

            var apiKey = await this._synaxisDbContext.OrganizationApiKeys
                .FirstOrDefaultAsync(k => k.Id == keyId && k.OrganizationId == organizationId, cancellationToken)
                .ConfigureAwait(false);

            if (apiKey == null)
            {
                return this.NotFound("API key not found");
            }

            if (!apiKey.IsActive || apiKey.RevokedAt.HasValue)
            {
                return this.StatusCode(410, "API key has already been revoked");
            }

            this._synaxisDbContext.OrganizationApiKeys.Remove(apiKey);
            await this._synaxisDbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

            return this.NoContent();
        }

        /// <summary>
        /// Rotates an API key.
        /// </summary>
        /// <param name="organizationId">The organization ID.</param>
        /// <param name="keyId">The API key ID.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>The rotated API key with the new key value.</returns>
        [HttpPost("{keyId}/rotate")]
        public async Task<IActionResult> RotateApiKey(
            Guid organizationId,
            Guid keyId,
            CancellationToken cancellationToken)
        {
            var userId = this.GetCurrentUserId();

            var organization = await this._synaxisDbContext.Organizations
                .FirstOrDefaultAsync(o => o.Id == organizationId, cancellationToken)
                .ConfigureAwait(false);

            if (organization == null)
            {
                return this.NotFound("Organization not found");
            }

            if (!await this.IsOrgAdminAsync(userId, organizationId, cancellationToken).ConfigureAwait(false))
            {
                return this.Forbid();
            }

            var apiKey = await this._synaxisDbContext.OrganizationApiKeys
                .FirstOrDefaultAsync(k => k.Id == keyId && k.OrganizationId == organizationId, cancellationToken)
                .ConfigureAwait(false);

            if (apiKey == null)
            {
                return this.NotFound("API key not found");
            }

            if (!apiKey.IsActive || apiKey.RevokedAt.HasValue)
            {
                return this.StatusCode(410, "API key has already been revoked");
            }

            var newKeyValue = GenerateSecureApiKey();
            var newKeyHash = ComputeSha256Hash(newKeyValue);
            var newKeyPrefix = newKeyValue.Substring(0, 8);

            apiKey.KeyHash = newKeyHash;
            apiKey.KeyPrefix = newKeyPrefix;
            apiKey.UpdatedAt = DateTime.UtcNow;

            await this._synaxisDbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

            var response = new RotateOrganizationApiKeyResponse
            {
                Id = apiKey.Id,
                Name = apiKey.Name,
                Key = newKeyValue,
                KeyPrefix = newKeyPrefix,
                Permissions = apiKey.Permissions,
                ExpiresAt = apiKey.ExpiresAt,
                IsActive = apiKey.IsActive,
                CreatedAt = apiKey.CreatedAt,
                RotatedAt = DateTime.UtcNow,
            };

            return this.Ok(response);
        }

        /// <summary>
        /// Gets usage statistics for an API key.
        /// </summary>
        /// <param name="organizationId">The organization ID.</param>
        /// <param name="keyId">The API key ID.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>The API key usage statistics.</returns>
        [HttpGet("{keyId}/usage")]
        public async Task<IActionResult> GetApiKeyUsage(
            Guid organizationId,
            Guid keyId,
            CancellationToken cancellationToken)
        {
            var userId = this.GetCurrentUserId();

            var organization = await this._synaxisDbContext.Organizations
                .FirstOrDefaultAsync(o => o.Id == organizationId, cancellationToken)
                .ConfigureAwait(false);

            if (organization == null)
            {
                return this.NotFound("Organization not found");
            }

            var isMember = await this._synaxisDbContext.TeamMemberships
                .AnyAsync(tm => tm.OrganizationId == organizationId && tm.UserId == userId, cancellationToken)
                .ConfigureAwait(false);

            if (!isMember)
            {
                return this.Forbid();
            }

            var apiKey = await this._synaxisDbContext.OrganizationApiKeys
                .FirstOrDefaultAsync(k => k.Id == keyId && k.OrganizationId == organizationId, cancellationToken)
                .ConfigureAwait(false);

            if (apiKey == null)
            {
                return this.NotFound("API key not found");
            }

            var response = this.BuildUsageResponse(apiKey);
            return this.Ok(response);
        }

        private (OrganizationApiKey ApiKey, string KeyValue) CreateApiKeyEntity(
            Guid organizationId,
            Guid userId,
            CreateOrganizationApiKeyRequest request)
        {
            var keyValue = GenerateSecureApiKey();
            var keyHash = ComputeSha256Hash(keyValue);
            var keyPrefix = keyValue.Substring(0, 8);

            var apiKey = new OrganizationApiKey
            {
                Id = Guid.NewGuid(),
                OrganizationId = organizationId,
                CreatedBy = userId,
                Name = request.Name ?? "API Key",
                KeyHash = keyHash,
                KeyPrefix = keyPrefix,
                Permissions = request.Permissions ?? new System.Collections.Generic.Dictionary<string, object>(),
                ExpiresAt = request.ExpiresAt,
                IsActive = true,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
            };

            return (apiKey, keyValue);
        }

        private OrganizationApiKeyUsageResponse BuildUsageResponse(OrganizationApiKey apiKey)
        {
            var now = DateTime.UtcNow;
            var oneHourAgo = now.AddHours(-1);
            var oneDayAgo = now.AddDays(-1);
            var oneWeekAgo = now.AddDays(-7);

            var totalRequests = apiKey.TotalRequests ?? 0;
            var errorCount = apiKey.ErrorCount ?? 0;
            var errorRate = totalRequests > 0 ? (double)errorCount / totalRequests : 0.0;

            return new OrganizationApiKeyUsageResponse
            {
                ApiKeyId = apiKey.Id,
                TotalRequests = totalRequests,
                ErrorCount = errorCount,
                ErrorRate = errorRate,
                LastUsedAt = apiKey.LastUsedAt,
                RequestsByHour = new System.Collections.Generic.Dictionary<string, int>
                {
                    { oneHourAgo.ToString("yyyy-MM-ddTHH:00:00Z", CultureInfo.InvariantCulture), 0 },
                },
                RequestsByDay = new System.Collections.Generic.Dictionary<string, int>
                {
                    { oneDayAgo.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture), 0 },
                },
                RequestsByWeek = new System.Collections.Generic.Dictionary<string, int>
                {
                    { oneWeekAgo.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture), 0 },
                },
            };
        }

        private static string GenerateSecureApiKey()
        {
            var bytes = new byte[32];
            RandomNumberGenerator.Fill(bytes);
            return "sk-" + Convert.ToHexString(bytes).ToLowerInvariant();
        }

        private static string ComputeSha256Hash(string input)
        {
            using var sha256 = SHA256.Create();
            var bytes = Encoding.UTF8.GetBytes(input);
            var hash = sha256.ComputeHash(bytes);
            return Convert.ToHexString(hash).ToLowerInvariant();
        }

        private static OrganizationApiKeyResponse MapToOrganizationApiKeyResponse(OrganizationApiKey apiKey)
        {
            return new OrganizationApiKeyResponse
            {
                Id = apiKey.Id,
                Name = apiKey.Name,
                KeyPrefix = apiKey.KeyPrefix,
                Permissions = apiKey.Permissions,
                ExpiresAt = apiKey.ExpiresAt,
                LastUsedAt = apiKey.LastUsedAt,
                RevokedAt = apiKey.RevokedAt,
                IsActive = apiKey.IsActive,
                CreatedAt = apiKey.CreatedAt,
                UpdatedAt = apiKey.UpdatedAt,
                CreatedBy = apiKey.Creator != null
                    ? new OrganizationApiKeyCreatorInfo
                    {
                        Id = apiKey.Creator.Id,
                        Email = apiKey.Creator.Email,
                        FirstName = apiKey.Creator.FirstName,
                        LastName = apiKey.Creator.LastName,
                    }
                    : null,
            };
        }

        private async Task<bool> IsOrgAdminAsync(Guid userId, Guid organizationId, CancellationToken cancellationToken)
        {
            var hasOrgAdminMembership = await this._synaxisDbContext.TeamMemberships
                .AnyAsync(tm => tm.UserId == userId && tm.OrganizationId == organizationId && tm.Role == "OrgAdmin", cancellationToken)
                .ConfigureAwait(false);

            if (hasOrgAdminMembership)
            {
                return true;
            }

            var user = await this._synaxisDbContext.Users
                .FirstOrDefaultAsync(u => u.Id == userId && u.OrganizationId == organizationId, cancellationToken)
                .ConfigureAwait(false);

            return user != null && (string.Equals(user.Role, "admin", StringComparison.OrdinalIgnoreCase) || string.Equals(user.Role, "owner", StringComparison.OrdinalIgnoreCase));
        }

        private Guid GetCurrentUserId()
        {
            return Guid.Parse(this.User.FindFirstValue(ClaimTypes.NameIdentifier) ?? this.User.FindFirstValue("sub")!);
        }
    }
}
