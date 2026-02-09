// <copyright file="OrganizationsController.cs" company="Synaxis">
// Copyright (c) Synaxis. All rights reserved.
// </copyright>

namespace Synaxis.InferenceGateway.WebApi.Controllers
{
    using System;
    using System.ComponentModel.DataAnnotations;
    using System.Linq;
    using System.Security.Claims;
    using System.Text.RegularExpressions;
    using System.Threading;
    using System.Threading.Tasks;
    using Microsoft.AspNetCore.Authorization;
    using Microsoft.AspNetCore.Cors;
    using Microsoft.AspNetCore.Mvc;
    using Microsoft.EntityFrameworkCore;
    using Synaxis.Core.Contracts;
    using Synaxis.Core.Models;
    using Synaxis.Infrastructure.Data;

    /// <summary>
    /// Controller for managing organizations.
    /// </summary>
    [ApiController]
    [Route("api/v1/organizations")]
    [Authorize]
    [EnableCors("WebApp")]
    public class OrganizationsController : ControllerBase
    {
        private readonly SynaxisDbContext _synaxisDbContext;

        /// <summary>
        /// Initializes a new instance of the <see cref="OrganizationsController"/> class.
        /// </summary>
        /// <param name="synaxisDbContext">The Synaxis database context.</param>
        public OrganizationsController(SynaxisDbContext synaxisDbContext)
        {
            this._synaxisDbContext = synaxisDbContext;
        }

        /// <summary>
        /// Creates a new organization.
        /// </summary>
        /// <param name="request">The create organization request.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>The created organization.</returns>
        [HttpPost]
        public async Task<IActionResult> CreateOrganization([FromBody] CreateOrganizationRequest request, CancellationToken cancellationToken)
        {
            var userId = Guid.Parse(this.User.FindFirstValue(ClaimTypes.NameIdentifier) ?? this.User.FindFirstValue("sub")!);

            var validationResult = this.ValidateOrganizationRequest(request);
            if (validationResult != null)
            {
                return validationResult;
            }

            var slugExists = await this._synaxisDbContext.Organizations
                .AnyAsync(o => o.Slug == request.Slug, cancellationToken)
                .ConfigureAwait(false);

            if (slugExists)
            {
                return this.BadRequest($"Slug '{request.Slug}' is already taken");
            }

            var organization = this.CreateOrganizationEntity(request);
            this._synaxisDbContext.Organizations.Add(organization);

            var (adminTeam, membership) = this.CreateAdminTeamAndMembership(organization.Id, userId);
            this._synaxisDbContext.Teams.Add(adminTeam);
            this._synaxisDbContext.TeamMemberships.Add(membership);

            await this._synaxisDbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

            var response = this.MapToOrganizationResponse(organization);
            return this.CreatedAtAction(nameof(this.GetOrganization), new { id = organization.Id }, response);
        }

        private IActionResult? ValidateOrganizationRequest(CreateOrganizationRequest request)
        {
            if (string.IsNullOrWhiteSpace(request.Name))
            {
                return this.BadRequest("Organization name is required");
            }

            if (string.IsNullOrWhiteSpace(request.Slug))
            {
                return this.BadRequest("Organization slug is required");
            }

            if (!Regex.IsMatch(request.Slug, "^[a-z0-9]+(-[a-z0-9]+)*$", RegexOptions.ExplicitCapture, TimeSpan.FromMilliseconds(100)))
            {
                return this.BadRequest("Slug must be lowercase alphanumeric with hyphens only");
            }

            return null;
        }

        private Organization CreateOrganizationEntity(CreateOrganizationRequest request)
        {
            return new Organization
            {
                Id = Guid.NewGuid(),
                Name = request.Name,
                Slug = request.Slug,
                Description = request.Description ?? string.Empty,
                PrimaryRegion = request.PrimaryRegion ?? "us-east-1",
                Tier = "free",
                BillingCurrency = "USD",
                CreditBalance = 0.00m,
                CreditCurrency = "USD",
                SubscriptionStatus = "active",
                IsTrial = false,
                DataRetentionDays = 30,
                RequireSso = false,
                IsActive = true,
                IsVerified = false,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
            };
        }

        private (Team AdminTeam, TeamMembership Membership) CreateAdminTeamAndMembership(Guid organizationId, Guid userId)
        {
            var adminTeam = new Team
            {
                Id = Guid.NewGuid(),
                OrganizationId = organizationId,
                Name = "Administrators",
                Slug = "administrators",
                Description = "Default administrator team",
                IsActive = true,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
            };

            var membership = new TeamMembership
            {
                Id = Guid.NewGuid(),
                UserId = userId,
                TeamId = adminTeam.Id,
                OrganizationId = organizationId,
                Role = "OrgAdmin",
                JoinedAt = DateTime.UtcNow,
            };

            return (adminTeam, membership);
        }

        private OrganizationResponse MapToOrganizationResponse(Organization organization)
        {
            return new OrganizationResponse
            {
                Id = organization.Id,
                Name = organization.Name,
                Slug = organization.Slug,
                Description = organization.Description,
                PrimaryRegion = organization.PrimaryRegion,
                Tier = organization.Tier,
                BillingCurrency = organization.BillingCurrency,
                CreditBalance = organization.CreditBalance,
                SubscriptionStatus = organization.SubscriptionStatus,
                IsActive = organization.IsActive,
                IsVerified = organization.IsVerified,
                CreatedAt = organization.CreatedAt,
                UpdatedAt = organization.UpdatedAt,
            };
        }

        /// <summary>
        /// Lists all organizations that the current user is a member of.
        /// </summary>
        /// <param name="page">The page number (1-based).</param>
        /// <param name="pageSize">The page size.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>A paginated list of organizations.</returns>
        [HttpGet]
        public async Task<IActionResult> ListOrganizations([FromQuery] int page = 1, [FromQuery] int pageSize = 10, CancellationToken cancellationToken = default)
        {
            var userId = Guid.Parse(this.User.FindFirstValue(ClaimTypes.NameIdentifier) ?? this.User.FindFirstValue("sub")!);

            var query = this._synaxisDbContext.Organizations
                .Where(o => o.Teams.Any(t => t.TeamMemberships.Any(tm => tm.UserId == userId)))
                .OrderBy(o => o.Name);

            var totalCount = await query.CountAsync(cancellationToken).ConfigureAwait(false);
            var items = await query
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(o => new OrganizationSummaryResponse
                {
                    Id = o.Id,
                    Name = o.Name,
                    Slug = o.Slug,
                    Description = o.Description,
                    Tier = o.Tier,
                    IsActive = o.IsActive,
                    CreatedAt = o.CreatedAt,
                })
                .ToListAsync(cancellationToken)
                .ConfigureAwait(false);

            return this.Ok(new
            {
                items,
                page,
                pageSize,
                totalCount,
                totalPages = (int)Math.Ceiling((double)totalCount / pageSize),
            });
        }

        /// <summary>
        /// Gets an organization by ID.
        /// </summary>
        /// <param name="id">The organization ID.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>The organization.</returns>
        [HttpGet("{id}")]
        public async Task<IActionResult> GetOrganization(Guid id, CancellationToken cancellationToken)
        {
            var userId = Guid.Parse(this.User.FindFirstValue(ClaimTypes.NameIdentifier) ?? this.User.FindFirstValue("sub")!);

            var organization = await this._synaxisDbContext.Organizations
                .FirstOrDefaultAsync(o => o.Id == id, cancellationToken)
                .ConfigureAwait(false);

            if (organization == null)
            {
                return this.NotFound();
            }

            var isMember = await this._synaxisDbContext.TeamMemberships
                .AnyAsync(tm => tm.OrganizationId == id && tm.UserId == userId, cancellationToken)
                .ConfigureAwait(false);

            if (!isMember)
            {
                return this.Forbid();
            }

            var response = this.MapToOrganizationResponse(organization);
            return this.Ok(response);
        }

        /// <summary>
        /// Updates an organization.
        /// </summary>
        /// <param name="id">The organization ID.</param>
        /// <param name="request">The update request.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>The updated organization.</returns>
        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateOrganization(Guid id, [FromBody] UpdateOrganizationRequest request, CancellationToken cancellationToken)
        {
            var userId = Guid.Parse(this.User.FindFirstValue(ClaimTypes.NameIdentifier) ?? this.User.FindFirstValue("sub")!);

            var organization = await this._synaxisDbContext.Organizations
                .FirstOrDefaultAsync(o => o.Id == id, cancellationToken)
                .ConfigureAwait(false);

            if (organization == null)
            {
                return this.NotFound();
            }

            var isOrgAdmin = await this.IsOrgAdminAsync(userId, id, cancellationToken).ConfigureAwait(false);

            if (!isOrgAdmin)
            {
                return this.Forbid();
            }

            if (!string.IsNullOrWhiteSpace(request.Name))
            {
                organization.Name = request.Name;
            }

            if (!string.IsNullOrWhiteSpace(request.Slug) && !string.Equals(request.Slug, organization.Slug, StringComparison.Ordinal))
            {
                var slugExists = await this._synaxisDbContext.Organizations
                    .AnyAsync(o => o.Slug == request.Slug && o.Id != id, cancellationToken)
                    .ConfigureAwait(false);

                if (slugExists)
                {
                    return this.BadRequest($"Slug '{request.Slug}' is already taken");
                }

                organization.Slug = request.Slug;
            }

            if (!string.IsNullOrWhiteSpace(request.Description))
            {
                organization.Description = request.Description;
            }

            organization.UpdatedAt = DateTime.UtcNow;
            await this._synaxisDbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

            var response = this.MapToOrganizationResponse(organization);
            return this.Ok(response);
        }

        /// <summary>
        /// Soft deletes an organization.
        /// </summary>
        /// <param name="id">The organization ID.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>No content.</returns>
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteOrganization(Guid id, CancellationToken cancellationToken)
        {
            var userId = Guid.Parse(this.User.FindFirstValue(ClaimTypes.NameIdentifier) ?? this.User.FindFirstValue("sub")!);

            var organization = await this._synaxisDbContext.Organizations
                .FirstOrDefaultAsync(o => o.Id == id, cancellationToken)
                .ConfigureAwait(false);

            if (organization == null)
            {
                return this.NotFound();
            }

            var isOrgAdmin = await this.IsOrgAdminAsync(userId, id, cancellationToken).ConfigureAwait(false);

            if (!isOrgAdmin)
            {
                return this.Forbid();
            }

            organization.IsActive = false;
            organization.UpdatedAt = DateTime.UtcNow;
            await this._synaxisDbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

            return this.NoContent();
        }

        /// <summary>
        /// Gets the limits for an organization.
        /// </summary>
        /// <param name="id">The organization ID.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>The organization limits.</returns>
        [HttpGet("{id}/limits")]
        public async Task<IActionResult> GetOrganizationLimits(Guid id, CancellationToken cancellationToken)
        {
            var userId = Guid.Parse(this.User.FindFirstValue(ClaimTypes.NameIdentifier) ?? this.User.FindFirstValue("sub")!);

            var organization = await this._synaxisDbContext.Organizations
                .FirstOrDefaultAsync(o => o.Id == id, cancellationToken)
                .ConfigureAwait(false);

            if (organization == null)
            {
                return this.NotFound();
            }

            var isOrgAdmin = await this.IsOrgAdminAsync(userId, id, cancellationToken).ConfigureAwait(false);

            if (!isOrgAdmin)
            {
                return this.Forbid();
            }

            var response = new OrganizationLimitsResponse
            {
                MaxTeams = organization.MaxTeams,
                MaxUsersPerTeam = organization.MaxUsersPerTeam,
                MaxKeysPerUser = organization.MaxKeysPerUser,
                MaxConcurrentRequests = organization.MaxConcurrentRequests,
                MonthlyRequestLimit = organization.MonthlyRequestLimit,
                MonthlyTokenLimit = organization.MonthlyTokenLimit,
            };

            return this.Ok(response);
        }

        /// <summary>
        /// Updates the limits for an organization.
        /// </summary>
        /// <param name="id">The organization ID.</param>
        /// <param name="request">The update limits request.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>The updated limits.</returns>
        [HttpPut("{id}/limits")]
        public async Task<IActionResult> UpdateOrganizationLimits(Guid id, [FromBody] UpdateOrganizationLimitsRequest request, CancellationToken cancellationToken)
        {
            var userId = Guid.Parse(this.User.FindFirstValue(ClaimTypes.NameIdentifier) ?? this.User.FindFirstValue("sub")!);

            var organization = await this._synaxisDbContext.Organizations
                .FirstOrDefaultAsync(o => o.Id == id, cancellationToken)
                .ConfigureAwait(false);

            if (organization == null)
            {
                return this.NotFound();
            }

            var isOrgAdmin = await this.IsOrgAdminAsync(userId, id, cancellationToken).ConfigureAwait(false);

            if (!isOrgAdmin)
            {
                return this.Forbid();
            }

            var validationResult = this.ValidateLimitsRequest(request);
            if (validationResult != null)
            {
                return validationResult;
            }

            ApplyLimitsToOrganization(organization, request);
            organization.UpdatedAt = DateTime.UtcNow;
            await this._synaxisDbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

            var response = this.MapToLimitsResponse(organization);
            return this.Ok(response);
        }

        /// <summary>
        /// Gets the settings for an organization.
        /// </summary>
        /// <param name="id">The organization ID.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>The organization settings.</returns>
        [HttpGet("{id}/settings")]
        public async Task<IActionResult> GetOrganizationSettings(Guid id, CancellationToken cancellationToken)
        {
            var userId = Guid.Parse(this.User.FindFirstValue(ClaimTypes.NameIdentifier) ?? this.User.FindFirstValue("sub")!);

            var organization = await this._synaxisDbContext.Organizations
                .FirstOrDefaultAsync(o => o.Id == id, cancellationToken)
                .ConfigureAwait(false);

            if (organization == null)
            {
                return this.NotFound();
            }

            var isMember = await this._synaxisDbContext.TeamMemberships
                .AnyAsync(tm => tm.OrganizationId == id && tm.UserId == userId, cancellationToken)
                .ConfigureAwait(false);

            if (!isMember)
            {
                return this.Forbid();
            }

            var response = new OrganizationSettingsResponse
            {
                Tier = organization.Tier,
                DataRetentionDays = organization.DataRetentionDays,
                RequireSso = organization.RequireSso,
                AllowedEmailDomains = organization.AllowedEmailDomains,
                AvailableRegions = organization.AvailableRegions,
                PrivacyConsent = organization.PrivacyConsent,
            };

            return this.Ok(response);
        }

        /// <summary>
        /// Updates the settings for an organization.
        /// </summary>
        /// <param name="id">The organization ID.</param>
        /// <param name="request">The update settings request.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>The updated settings.</returns>
        [HttpPut("{id}/settings")]
        public async Task<IActionResult> UpdateOrganizationSettings(Guid id, [FromBody] UpdateOrganizationSettingsRequest request, CancellationToken cancellationToken)
        {
            var userId = Guid.Parse(this.User.FindFirstValue(ClaimTypes.NameIdentifier) ?? this.User.FindFirstValue("sub")!);

            var organization = await this._synaxisDbContext.Organizations
                .FirstOrDefaultAsync(o => o.Id == id, cancellationToken)
                .ConfigureAwait(false);

            if (organization == null)
            {
                return this.NotFound();
            }

            var isOrgAdmin = await this.IsOrgAdminAsync(userId, id, cancellationToken).ConfigureAwait(false);

            if (!isOrgAdmin)
            {
                return this.Forbid();
            }

            if (request.DataRetentionDays.HasValue)
            {
                organization.DataRetentionDays = request.DataRetentionDays.Value;
            }

            if (request.RequireSso.HasValue)
            {
                organization.RequireSso = request.RequireSso.Value;
            }

            if (request.AllowedEmailDomains != null)
            {
                organization.AllowedEmailDomains = request.AllowedEmailDomains;
            }

            organization.UpdatedAt = DateTime.UtcNow;
            await this._synaxisDbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

            var response = new OrganizationSettingsResponse
            {
                Tier = organization.Tier,
                DataRetentionDays = organization.DataRetentionDays,
                RequireSso = organization.RequireSso,
                AllowedEmailDomains = organization.AllowedEmailDomains,
                AvailableRegions = organization.AvailableRegions,
                PrivacyConsent = organization.PrivacyConsent,
            };

            return this.Ok(response);
        }

        private IActionResult? ValidateLimitsRequest(UpdateOrganizationLimitsRequest request)
        {
            if (request.MaxTeams.HasValue && request.MaxTeams < 0)
            {
                return this.BadRequest("MaxTeams cannot be negative");
            }

            if (request.MaxUsersPerTeam.HasValue && request.MaxUsersPerTeam < 0)
            {
                return this.BadRequest("MaxUsersPerTeam cannot be negative");
            }

            if (request.MaxKeysPerUser.HasValue && request.MaxKeysPerUser < 0)
            {
                return this.BadRequest("MaxKeysPerUser cannot be negative");
            }

            if (request.MaxConcurrentRequests.HasValue && request.MaxConcurrentRequests < 0)
            {
                return this.BadRequest("MaxConcurrentRequests cannot be negative");
            }

            if (request.MonthlyRequestLimit.HasValue && request.MonthlyRequestLimit < 0)
            {
                return this.BadRequest("MonthlyRequestLimit cannot be negative");
            }

            if (request.MonthlyTokenLimit.HasValue && request.MonthlyTokenLimit < 0)
            {
                return this.BadRequest("MonthlyTokenLimit cannot be negative");
            }

            return null;
        }

        private static void ApplyLimitsToOrganization(Organization organization, UpdateOrganizationLimitsRequest request)
        {
            if (request.MaxTeams.HasValue)
            {
                organization.MaxTeams = request.MaxTeams;
            }

            if (request.MaxUsersPerTeam.HasValue)
            {
                organization.MaxUsersPerTeam = request.MaxUsersPerTeam;
            }

            if (request.MaxKeysPerUser.HasValue)
            {
                organization.MaxKeysPerUser = request.MaxKeysPerUser;
            }

            if (request.MaxConcurrentRequests.HasValue)
            {
                organization.MaxConcurrentRequests = request.MaxConcurrentRequests;
            }

            if (request.MonthlyRequestLimit.HasValue)
            {
                organization.MonthlyRequestLimit = request.MonthlyRequestLimit;
            }

            if (request.MonthlyTokenLimit.HasValue)
            {
                organization.MonthlyTokenLimit = request.MonthlyTokenLimit;
            }
        }

        private OrganizationLimitsResponse MapToLimitsResponse(Organization organization)
        {
            return new OrganizationLimitsResponse
            {
                MaxTeams = organization.MaxTeams,
                MaxUsersPerTeam = organization.MaxUsersPerTeam,
                MaxKeysPerUser = organization.MaxKeysPerUser,
                MaxConcurrentRequests = organization.MaxConcurrentRequests,
                MonthlyRequestLimit = organization.MonthlyRequestLimit,
                MonthlyTokenLimit = organization.MonthlyTokenLimit,
            };
        }

        private async Task<bool> IsOrgAdminAsync(Guid userId, Guid organizationId, CancellationToken cancellationToken)
        {
            // Check if user has OrgAdmin role in TeamMemberships
            var hasOrgAdminMembership = await this._synaxisDbContext.TeamMemberships
                .AnyAsync(tm => tm.UserId == userId && tm.OrganizationId == organizationId && tm.Role == "OrgAdmin", cancellationToken)
                .ConfigureAwait(false);

            if (hasOrgAdminMembership)
            {
                return true;
            }

            // Check if user has admin/owner role in User table
            var user = await this._synaxisDbContext.Users
                .FirstOrDefaultAsync(u => u.Id == userId && u.OrganizationId == organizationId, cancellationToken)
                .ConfigureAwait(false);

            return user != null && (string.Equals(user.Role, "admin", StringComparison.OrdinalIgnoreCase) || string.Equals(user.Role, "owner", StringComparison.OrdinalIgnoreCase));
        }
    }

    /// <summary>
    /// Request to create a new organization.
    /// </summary>
    public class CreateOrganizationRequest
    {
        /// <summary>
        /// Gets or sets the organization name.
        /// </summary>
        [Required]
        [StringLength(255)]
        public string Name { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the organization slug.
        /// </summary>
        [Required]
        [StringLength(100)]
        public string Slug { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the organization description.
        /// </summary>
        public string? Description { get; set; }

        /// <summary>
        /// Gets or sets the primary region.
        /// </summary>
        public string? PrimaryRegion { get; set; }
    }

    /// <summary>
    /// Organization response model.
    /// </summary>
    public class OrganizationResponse
    {
        /// <summary>
        /// Gets or sets the organization ID.
        /// </summary>
        public Guid Id { get; set; }

        /// <summary>
        /// Gets or sets the organization name.
        /// </summary>
        public string Name { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the organization slug.
        /// </summary>
        public string Slug { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the organization description.
        /// </summary>
        public string Description { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the primary region.
        /// </summary>
        public string PrimaryRegion { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the subscription tier.
        /// </summary>
        public string Tier { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the billing currency.
        /// </summary>
        public string BillingCurrency { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the credit balance.
        /// </summary>
        public decimal CreditBalance { get; set; }

        /// <summary>
        /// Gets or sets the subscription status.
        /// </summary>
        public string SubscriptionStatus { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets a value indicating whether the organization is active.
        /// </summary>
        public bool IsActive { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether the organization is verified.
        /// </summary>
        public bool IsVerified { get; set; }

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
    /// Organization summary response for list endpoints.
    /// </summary>
    public class OrganizationSummaryResponse
    {
        /// <summary>
        /// Gets or sets the organization ID.
        /// </summary>
        public Guid Id { get; set; }

        /// <summary>
        /// Gets or sets the organization name.
        /// </summary>
        public string Name { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the organization slug.
        /// </summary>
        public string Slug { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the organization description.
        /// </summary>
        public string Description { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the subscription tier.
        /// </summary>
        public string Tier { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets a value indicating whether the organization is active.
        /// </summary>
        public bool IsActive { get; set; }

        /// <summary>
        /// Gets or sets the creation timestamp.
        /// </summary>
        public DateTime CreatedAt { get; set; }
    }

    /// <summary>
    /// Request to update an organization.
    /// </summary>
    public class UpdateOrganizationRequest
    {
        /// <summary>
        /// Gets or sets the organization name.
        /// </summary>
        public string? Name { get; set; }

        /// <summary>
        /// Gets or sets the organization slug.
        /// </summary>
        public string? Slug { get; set; }

        /// <summary>
        /// Gets or sets the organization description.
        /// </summary>
        public string? Description { get; set; }
    }
}
