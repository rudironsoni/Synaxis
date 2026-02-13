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
        private readonly IPasswordService _passwordService;

        /// <summary>
        /// Initializes a new instance of the <see cref="OrganizationsController"/> class.
        /// </summary>
        /// <param name="synaxisDbContext">The Synaxis database context.</param>
        /// <param name="passwordService">The password service.</param>
        public OrganizationsController(SynaxisDbContext synaxisDbContext, IPasswordService passwordService)
        {
            this._synaxisDbContext = synaxisDbContext;
            this._passwordService = passwordService;
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

        /// <summary>
        /// Gets the password policy for an organization.
        /// </summary>
        /// <param name="id">The organization ID.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>The password policy.</returns>
        [HttpGet("{id}/password-policy")]
        public async Task<IActionResult> GetPasswordPolicy(Guid id, CancellationToken cancellationToken)
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

            var policy = await this._passwordService.GetPasswordPolicyAsync(id);

            var response = new PasswordPolicyResponse
            {
                Id = policy.Id,
                OrganizationId = policy.OrganizationId,
                MinLength = policy.MinLength,
                RequireUppercase = policy.RequireUppercase,
                RequireLowercase = policy.RequireLowercase,
                RequireNumbers = policy.RequireNumbers,
                RequireSpecialCharacters = policy.RequireSpecialCharacters,
                PasswordHistoryCount = policy.PasswordHistoryCount,
                PasswordExpirationDays = policy.PasswordExpirationDays,
                PasswordExpirationWarningDays = policy.PasswordExpirationWarningDays,
                MaxFailedChangeAttempts = policy.MaxFailedChangeAttempts,
                LockoutDurationMinutes = policy.LockoutDurationMinutes,
                BlockCommonPasswords = policy.BlockCommonPasswords,
                BlockUserInfoInPassword = policy.BlockUserInfoInPassword,
                CreatedAt = policy.CreatedAt,
                UpdatedAt = policy.UpdatedAt,
            };

            return this.Ok(response);
        }

        /// <summary>
        /// Updates the password policy for an organization.
        /// </summary>
        /// <param name="id">The organization ID.</param>
        /// <param name="request">The update password policy request.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>The updated password policy.</returns>
        [HttpPut("{id}/password-policy")]
        public async Task<IActionResult> UpdatePasswordPolicy(Guid id, [FromBody] UpdatePasswordPolicyRequest request, CancellationToken cancellationToken)
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

            var validationResult = this.ValidatePasswordPolicyRequest(request);
            if (validationResult != null)
            {
                return validationResult;
            }

            var policy = this.CreatePasswordPolicyFromRequest(request);
            var updatedPolicy = await this._passwordService.UpdatePasswordPolicyAsync(id, policy);
            var response = this.MapToPasswordPolicyResponse(updatedPolicy);

            return this.Ok(response);
        }

        private Synaxis.Core.Models.PasswordPolicy CreatePasswordPolicyFromRequest(UpdatePasswordPolicyRequest request)
        {
            return new Synaxis.Core.Models.PasswordPolicy
            {
                MinLength = request.MinLength,
                RequireUppercase = request.RequireUppercase,
                RequireLowercase = request.RequireLowercase,
                RequireNumbers = request.RequireNumbers,
                RequireSpecialCharacters = request.RequireSpecialCharacters,
                PasswordHistoryCount = request.PasswordHistoryCount,
                PasswordExpirationDays = request.PasswordExpirationDays,
                PasswordExpirationWarningDays = request.PasswordExpirationWarningDays,
                MaxFailedChangeAttempts = request.MaxFailedChangeAttempts,
                LockoutDurationMinutes = request.LockoutDurationMinutes,
                BlockCommonPasswords = request.BlockCommonPasswords,
                BlockUserInfoInPassword = request.BlockUserInfoInPassword,
            };
        }

        private PasswordPolicyResponse MapToPasswordPolicyResponse(Synaxis.Core.Models.PasswordPolicy policy)
        {
            return new PasswordPolicyResponse
            {
                Id = policy.Id,
                OrganizationId = policy.OrganizationId,
                MinLength = policy.MinLength,
                RequireUppercase = policy.RequireUppercase,
                RequireLowercase = policy.RequireLowercase,
                RequireNumbers = policy.RequireNumbers,
                RequireSpecialCharacters = policy.RequireSpecialCharacters,
                PasswordHistoryCount = policy.PasswordHistoryCount,
                PasswordExpirationDays = policy.PasswordExpirationDays,
                PasswordExpirationWarningDays = policy.PasswordExpirationWarningDays,
                MaxFailedChangeAttempts = policy.MaxFailedChangeAttempts,
                LockoutDurationMinutes = policy.LockoutDurationMinutes,
                BlockCommonPasswords = policy.BlockCommonPasswords,
                BlockUserInfoInPassword = policy.BlockUserInfoInPassword,
                CreatedAt = policy.CreatedAt,
                UpdatedAt = policy.UpdatedAt,
            };
        }

        private IActionResult? ValidatePasswordPolicyRequest(UpdatePasswordPolicyRequest request)
        {
            if (request.MinLength < 8 || request.MinLength > 128)
            {
                return this.BadRequest("MinLength must be between 8 and 128");
            }

            if (request.PasswordHistoryCount < 0 || request.PasswordHistoryCount > 24)
            {
                return this.BadRequest("PasswordHistoryCount must be between 0 and 24");
            }

            if (request.PasswordExpirationDays < 0 || request.PasswordExpirationDays > 365)
            {
                return this.BadRequest("PasswordExpirationDays must be between 0 and 365");
            }

            if (request.PasswordExpirationWarningDays < 0 || request.PasswordExpirationWarningDays > 30)
            {
                return this.BadRequest("PasswordExpirationWarningDays must be between 0 and 30");
            }

            if (request.MaxFailedChangeAttempts < 3 || request.MaxFailedChangeAttempts > 10)
            {
                return this.BadRequest("MaxFailedChangeAttempts must be between 3 and 10");
            }

            if (request.LockoutDurationMinutes < 5 || request.LockoutDurationMinutes > 60)
            {
                return this.BadRequest("LockoutDurationMinutes must be between 5 and 60");
            }

            return null;
        }

        /// <summary>
        /// Creates a new API key for an organization.
        /// </summary>
        /// <param name="id">The organization ID.</param>
        /// <param name="request">The create API key request.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>The created API key with the actual key value.</returns>
        [HttpPost("{id}/api-keys")]
        public async Task<IActionResult> CreateApiKey(Guid id, [FromBody] CreateOrganizationApiKeyRequest request, CancellationToken cancellationToken)
        {
            var userId = Guid.Parse(this.User.FindFirstValue(ClaimTypes.NameIdentifier) ?? this.User.FindFirstValue("sub")!);

            var organization = await this._synaxisDbContext.Organizations
                .FirstOrDefaultAsync(o => o.Id == id, cancellationToken)
                .ConfigureAwait(false);

            if (organization == null)
            {
                return this.NotFound("Organization not found");
            }

            var isOrgAdmin = await this.IsOrgAdminAsync(userId, id, cancellationToken).ConfigureAwait(false);

            if (!isOrgAdmin)
            {
                return this.Forbid();
            }

            var (apiKey, keyValue) = this.CreateApiKeyEntity(id, userId, request);
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

            return this.CreatedAtAction(nameof(this.GetApiKey), new { id = id, keyId = apiKey.Id }, response);
        }

        private (OrganizationApiKey ApiKey, string KeyValue) CreateApiKeyEntity(Guid organizationId, Guid userId, CreateOrganizationApiKeyRequest request)
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
                Permissions = request.Permissions ?? new Dictionary<string, object>(),
                ExpiresAt = request.ExpiresAt,
                IsActive = true,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
            };

            return (apiKey, keyValue);
        }

        /// <summary>
        /// Lists API keys for an organization.
        /// </summary>
        /// <param name="id">The organization ID.</param>
        /// <param name="page">The page number (1-based).</param>
        /// <param name="pageSize">The page size.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>A paginated list of API keys.</returns>
        [HttpGet("{id}/api-keys")]
        public async Task<IActionResult> ListApiKeys(Guid id, [FromQuery] int page = 1, [FromQuery] int pageSize = 10, CancellationToken cancellationToken = default)
        {
            var userId = Guid.Parse(this.User.FindFirstValue(ClaimTypes.NameIdentifier) ?? this.User.FindFirstValue("sub")!);

            var organization = await this._synaxisDbContext.Organizations
                .FirstOrDefaultAsync(o => o.Id == id, cancellationToken)
                .ConfigureAwait(false);

            if (organization == null)
            {
                return this.NotFound("Organization not found");
            }

            var isMember = await this._synaxisDbContext.TeamMemberships
                .AnyAsync(tm => tm.OrganizationId == id && tm.UserId == userId, cancellationToken)
                .ConfigureAwait(false);

            if (!isMember)
            {
                return this.Forbid();
            }

            var query = this._synaxisDbContext.OrganizationApiKeys
                .Where(k => k.OrganizationId == id)
                .Include(k => k.Creator);

            var totalCount = await query.CountAsync(cancellationToken).ConfigureAwait(false);

            var apiKeys = await query
                .OrderByDescending(k => k.CreatedAt)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(k => OrganizationsController.MapToOrganizationApiKeyResponse(k))
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
        /// <param name="id">The organization ID.</param>
        /// <param name="keyId">The API key ID.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>The API key details.</returns>
        [HttpGet("{id}/api-keys/{keyId}")]
        public async Task<IActionResult> GetApiKey(Guid id, Guid keyId, CancellationToken cancellationToken)
        {
            var userId = Guid.Parse(this.User.FindFirstValue(ClaimTypes.NameIdentifier) ?? this.User.FindFirstValue("sub")!);

            var organization = await this._synaxisDbContext.Organizations
                .FirstOrDefaultAsync(o => o.Id == id, cancellationToken)
                .ConfigureAwait(false);

            if (organization == null)
            {
                return this.NotFound("Organization not found");
            }

            var isMember = await this._synaxisDbContext.TeamMemberships
                .AnyAsync(tm => tm.OrganizationId == id && tm.UserId == userId, cancellationToken)
                .ConfigureAwait(false);

            if (!isMember)
            {
                return this.Forbid();
            }

            var apiKey = await this._synaxisDbContext.OrganizationApiKeys
                .Include(k => k.Creator)
                .FirstOrDefaultAsync(k => k.Id == keyId && k.OrganizationId == id, cancellationToken)
                .ConfigureAwait(false);

            if (apiKey == null)
            {
                return this.NotFound("API key not found");
            }

            var response = OrganizationsController.MapToOrganizationApiKeyResponse(apiKey);
            return this.Ok(response);
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

        /// <summary>
        /// Updates an API key.
        /// </summary>
        /// <param name="id">The organization ID.</param>
        /// <param name="keyId">The API key ID.</param>
        /// <param name="request">The update API key request.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>The updated API key.</returns>
        [HttpPut("{id}/api-keys/{keyId}")]
        public async Task<IActionResult> UpdateApiKey(Guid id, Guid keyId, [FromBody] UpdateOrganizationApiKeyRequest request, CancellationToken cancellationToken)
        {
            var userId = Guid.Parse(this.User.FindFirstValue(ClaimTypes.NameIdentifier) ?? this.User.FindFirstValue("sub")!);

            var organization = await this._synaxisDbContext.Organizations
                .FirstOrDefaultAsync(o => o.Id == id, cancellationToken)
                .ConfigureAwait(false);

            if (organization == null)
            {
                return this.NotFound("Organization not found");
            }

            var isOrgAdmin = await this.IsOrgAdminAsync(userId, id, cancellationToken).ConfigureAwait(false);

            if (!isOrgAdmin)
            {
                return this.Forbid();
            }

            var apiKey = await this._synaxisDbContext.OrganizationApiKeys
                .FirstOrDefaultAsync(k => k.Id == keyId && k.OrganizationId == id, cancellationToken)
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

            var response = OrganizationsController.MapToOrganizationApiKeyResponse(apiKey);
            return this.Ok(response);
        }

        private static string GenerateSecureApiKey()
        {
            return "sk-" + Guid.NewGuid().ToString("N").Substring(0, 32);
        }

        private static string ComputeSha256Hash(string input)
        {
            using var sha256 = System.Security.Cryptography.SHA256.Create();
            var bytes = System.Text.Encoding.UTF8.GetBytes(input);
            var hash = sha256.ComputeHash(bytes);
            return Convert.ToHexString(hash).ToLowerInvariant();
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

    /// <summary>
    /// Password policy response model.
    /// </summary>
    public class PasswordPolicyResponse
    {
        /// <summary>
        /// Gets or sets the password policy ID.
        /// </summary>
        public Guid Id { get; set; }

        /// <summary>
        /// Gets or sets the organization ID.
        /// </summary>
        public Guid OrganizationId { get; set; }

        /// <summary>
        /// Gets or sets the minimum password length.
        /// </summary>
        public int MinLength { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether password must contain uppercase letters.
        /// </summary>
        public bool RequireUppercase { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether password must contain lowercase letters.
        /// </summary>
        public bool RequireLowercase { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether password must contain numbers.
        /// </summary>
        public bool RequireNumbers { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether password must contain special characters.
        /// </summary>
        public bool RequireSpecialCharacters { get; set; }

        /// <summary>
        /// Gets or sets the number of previous passwords to remember for history check.
        /// </summary>
        public int PasswordHistoryCount { get; set; }

        /// <summary>
        /// Gets or sets the password expiration period in days (0 = never expires).
        /// </summary>
        public int PasswordExpirationDays { get; set; }

        /// <summary>
        /// Gets or sets the number of days before expiration to show warning.
        /// </summary>
        public int PasswordExpirationWarningDays { get; set; }

        /// <summary>
        /// Gets or sets the maximum number of failed password change attempts before lockout.
        /// </summary>
        public int MaxFailedChangeAttempts { get; set; }

        /// <summary>
        /// Gets or sets the lockout duration in minutes after failed attempts.
        /// </summary>
        public int LockoutDurationMinutes { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether common passwords are blocked.
        /// </summary>
        public bool BlockCommonPasswords { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether user info (email, name) is blocked in password.
        /// </summary>
        public bool BlockUserInfoInPassword { get; set; }

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
    /// Request to update password policy.
    /// </summary>
    public class UpdatePasswordPolicyRequest
    {
        /// <summary>
        /// Gets or sets the minimum password length.
        /// </summary>
        [Range(8, 128)]
        public int MinLength { get; set; } = 12;

        /// <summary>
        /// Gets or sets a value indicating whether password must contain uppercase letters.
        /// </summary>
        public bool RequireUppercase { get; set; } = true;

        /// <summary>
        /// Gets or sets a value indicating whether password must contain lowercase letters.
        /// </summary>
        public bool RequireLowercase { get; set; } = true;

        /// <summary>
        /// Gets or sets a value indicating whether password must contain numbers.
        /// </summary>
        public bool RequireNumbers { get; set; } = true;

        /// <summary>
        /// Gets or sets a value indicating whether password must contain special characters.
        /// </summary>
        public bool RequireSpecialCharacters { get; set; } = true;

        /// <summary>
        /// Gets or sets the number of previous passwords to remember for history check.
        /// </summary>
        [Range(0, 24)]
        public int PasswordHistoryCount { get; set; } = 5;

        /// <summary>
        /// Gets or sets the password expiration period in days (0 = never expires).
        /// </summary>
        [Range(0, 365)]
        public int PasswordExpirationDays { get; set; } = 90;

        /// <summary>
        /// Gets or sets the number of days before expiration to show warning.
        /// </summary>
        [Range(0, 30)]
        public int PasswordExpirationWarningDays { get; set; } = 14;

        /// <summary>
        /// Gets or sets the maximum number of failed password change attempts before lockout.
        /// </summary>
        [Range(3, 10)]
        public int MaxFailedChangeAttempts { get; set; } = 5;

        /// <summary>
        /// Gets or sets the lockout duration in minutes after failed attempts.
        /// </summary>
        [Range(5, 60)]
        public int LockoutDurationMinutes { get; set; } = 15;

        /// <summary>
        /// Gets or sets a value indicating whether common passwords are blocked.
        /// </summary>
        public bool BlockCommonPasswords { get; set; } = true;

        /// <summary>
        /// Gets or sets a value indicating whether user info (email, name) is blocked in password.
        /// </summary>
        public bool BlockUserInfoInPassword { get; set; } = true;
    }

    /// <summary>
    /// Request to create a new organization API key.
    /// </summary>
    public class CreateOrganizationApiKeyRequest
    {
        /// <summary>
        /// Gets or sets the API key name.
        /// </summary>
        [StringLength(255)]
        public string? Name { get; set; }

        /// <summary>
        /// Gets or sets the permissions.
        /// </summary>
        public IDictionary<string, object>? Permissions { get; set; }

        /// <summary>
        /// Gets or sets the expiration timestamp (null = never expires).
        /// </summary>
        public DateTime? ExpiresAt { get; set; }
    }

    /// <summary>
    /// Response for creating an organization API key (includes the actual key value).
    /// </summary>
    public class CreateOrganizationApiKeyResponse
    {
        /// <summary>
        /// Gets or sets the API key ID.
        /// </summary>
        public Guid Id { get; set; }

        /// <summary>
        /// Gets or sets the API key name.
        /// </summary>
        public string Name { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the actual API key value (shown only once).
        /// </summary>
        public string Key { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the key prefix.
        /// </summary>
        public string KeyPrefix { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the permissions.
        /// </summary>
        public IDictionary<string, object> Permissions { get; set; } = new Dictionary<string, object>();

        /// <summary>
        /// Gets or sets the expiration timestamp.
        /// </summary>
        public DateTime? ExpiresAt { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether the key is active.
        /// </summary>
        public bool IsActive { get; set; }

        /// <summary>
        /// Gets or sets the creation timestamp.
        /// </summary>
        public DateTime CreatedAt { get; set; }
    }

    /// <summary>
    /// Response for organization API key details (does NOT include the actual key value).
    /// </summary>
    public class OrganizationApiKeyResponse
    {
        /// <summary>
        /// Gets or sets the API key ID.
        /// </summary>
        public Guid Id { get; set; }

        /// <summary>
        /// Gets or sets the API key name.
        /// </summary>
        public string Name { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the key prefix.
        /// </summary>
        public string KeyPrefix { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the permissions.
        /// </summary>
        public IDictionary<string, object> Permissions { get; set; } = new Dictionary<string, object>();

        /// <summary>
        /// Gets or sets the expiration timestamp.
        /// </summary>
        public DateTime? ExpiresAt { get; set; }

        /// <summary>
        /// Gets or sets the last used timestamp.
        /// </summary>
        public DateTime? LastUsedAt { get; set; }

        /// <summary>
        /// Gets or sets the revocation timestamp.
        /// </summary>
        public DateTime? RevokedAt { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether the key is active.
        /// </summary>
        public bool IsActive { get; set; }

        /// <summary>
        /// Gets or sets the creation timestamp.
        /// </summary>
        public DateTime CreatedAt { get; set; }

        /// <summary>
        /// Gets or sets the last update timestamp.
        /// </summary>
        public DateTime UpdatedAt { get; set; }

        /// <summary>
        /// Gets or sets the creator information.
        /// </summary>
        public OrganizationApiKeyCreatorInfo? CreatedBy { get; set; }
    }

    /// <summary>
    /// Creator information for organization API keys.
    /// </summary>
    public class OrganizationApiKeyCreatorInfo
    {
        /// <summary>
        /// Gets or sets the user ID.
        /// </summary>
        public Guid Id { get; set; }

        /// <summary>
        /// Gets or sets the email address.
        /// </summary>
        public string Email { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the first name.
        /// </summary>
        public string? FirstName { get; set; }

        /// <summary>
        /// Gets or sets the last name.
        /// </summary>
        public string? LastName { get; set; }
    }

    /// <summary>
    /// Request to update an organization API key.
    /// </summary>
    public class UpdateOrganizationApiKeyRequest
    {
        /// <summary>
        /// Gets or sets the API key name.
        /// </summary>
        [StringLength(255)]
        public string? Name { get; set; }

        /// <summary>
        /// Gets or sets the permissions.
        /// </summary>
        public IDictionary<string, object>? Permissions { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether to revoke the key.
        /// </summary>
        public bool? Revoke { get; set; }

        /// <summary>
        /// Gets or sets the revocation reason.
        /// </summary>
        [StringLength(500)]
        public string? RevokedReason { get; set; }
    }
}
