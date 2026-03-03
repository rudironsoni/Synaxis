// <copyright file="OrganizationsController.cs" company="Synaxis">
// Copyright (c) Synaxis. All rights reserved.
// </copyright>

namespace Synaxis.InferenceGateway.WebApi.Controllers
{
    using System;
    using System.Linq;
    using System.Security.Claims;
    using System.Text.RegularExpressions;
    using System.Threading;
    using System.Threading.Tasks;
    using Microsoft.AspNetCore.Authorization;
    using Microsoft.AspNetCore.Cors;
    using Microsoft.AspNetCore.Mvc;
    using Microsoft.EntityFrameworkCore;
    using Synaxis.Core.Models;
    using Synaxis.InferenceGateway.WebApi.Controllers.Organizations;
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
        public async Task<IActionResult> CreateOrganization(
            [FromBody] CreateOrganizationRequest request,
            CancellationToken cancellationToken)
        {
            var userId = this.GetCurrentUserId();

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
            return this.CreatedAtAction(
                nameof(this.GetOrganization),
                new { id = organization.Id },
                response);
        }

        /// <summary>
        /// Lists all organizations that the current user is a member of.
        /// </summary>
        /// <param name="page">The page number (1-based).</param>
        /// <param name="pageSize">The page size.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>A paginated list of organizations.</returns>
        [HttpGet]
        public async Task<IActionResult> ListOrganizations(
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 10,
            CancellationToken cancellationToken = default)
        {
            var userId = this.GetCurrentUserId();

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
            var userId = this.GetCurrentUserId();

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
        public async Task<IActionResult> UpdateOrganization(
            Guid id,
            [FromBody] UpdateOrganizationRequest request,
            CancellationToken cancellationToken)
        {
            var userId = this.GetCurrentUserId();

            var organization = await this._synaxisDbContext.Organizations
                .FirstOrDefaultAsync(o => o.Id == id, cancellationToken)
                .ConfigureAwait(false);

            if (organization == null)
            {
                return this.NotFound();
            }

            if (!await this.IsOrgAdminAsync(userId, id, cancellationToken).ConfigureAwait(false))
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
            var userId = this.GetCurrentUserId();

            var organization = await this._synaxisDbContext.Organizations
                .FirstOrDefaultAsync(o => o.Id == id, cancellationToken)
                .ConfigureAwait(false);

            if (organization == null)
            {
                return this.NotFound();
            }

            if (!await this.IsOrgAdminAsync(userId, id, cancellationToken).ConfigureAwait(false))
            {
                return this.Forbid();
            }

            organization.IsActive = false;
            organization.UpdatedAt = DateTime.UtcNow;
            await this._synaxisDbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

            return this.NoContent();
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
