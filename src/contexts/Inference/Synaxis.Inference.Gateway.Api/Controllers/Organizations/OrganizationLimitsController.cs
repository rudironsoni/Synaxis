// <copyright file="OrganizationLimitsController.cs" company="Synaxis">
// Copyright (c) Synaxis. All rights reserved.
// </copyright>

namespace Synaxis.InferenceGateway.WebApi.Controllers.Organizations
{
    using System;
    using System.Linq;
    using System.Security.Claims;
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
    /// Controller for managing organization limits.
    /// </summary>
    [ApiController]
    [Route("api/v1/organizations/{organizationId}/limits")]
    [Authorize]
    [EnableCors("WebApp")]
    public class OrganizationLimitsController : ControllerBase
    {
        private readonly SynaxisDbContext _synaxisDbContext;

        /// <summary>
        /// Initializes a new instance of the <see cref="OrganizationLimitsController"/> class.
        /// </summary>
        /// <param name="synaxisDbContext">The Synaxis database context.</param>
        public OrganizationLimitsController(SynaxisDbContext synaxisDbContext)
        {
            this._synaxisDbContext = synaxisDbContext;
        }

        /// <summary>
        /// Gets the limits for an organization.
        /// </summary>
        /// <param name="organizationId">The organization ID.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>The organization limits.</returns>
        [HttpGet]
        public async Task<IActionResult> GetOrganizationLimits(Guid organizationId, CancellationToken cancellationToken)
        {
            var userId = this.GetCurrentUserId();

            var organization = await this._synaxisDbContext.Organizations
                .FirstOrDefaultAsync(o => o.Id == organizationId, cancellationToken)
                .ConfigureAwait(false);

            if (organization == null)
            {
                return this.NotFound();
            }

            if (!await this.IsOrgAdminAsync(userId, organizationId, cancellationToken).ConfigureAwait(false))
            {
                return this.Forbid();
            }

            var response = MapToLimitsResponse(organization);
            return this.Ok(response);
        }

        /// <summary>
        /// Updates the limits for an organization.
        /// </summary>
        /// <param name="organizationId">The organization ID.</param>
        /// <param name="request">The update limits request.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>The updated limits.</returns>
        [HttpPut]
        public async Task<IActionResult> UpdateOrganizationLimits(
            Guid organizationId,
            [FromBody] UpdateOrganizationLimitsRequest request,
            CancellationToken cancellationToken)
        {
            var userId = this.GetCurrentUserId();

            var organization = await this._synaxisDbContext.Organizations
                .FirstOrDefaultAsync(o => o.Id == organizationId, cancellationToken)
                .ConfigureAwait(false);

            if (organization == null)
            {
                return this.NotFound();
            }

            if (!await this.IsOrgAdminAsync(userId, organizationId, cancellationToken).ConfigureAwait(false))
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

            var response = MapToLimitsResponse(organization);
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

        private static OrganizationLimitsResponse MapToLimitsResponse(Organization organization)
        {
            return new OrganizationLimitsResponse
            {
                MaxTeams = organization.MaxTeams,
                MaxUsersPerTeam = organization.MaxUsersPerTeam,
                MaxKeysPerUser = organization.MaxKeysPerUser,
                MaxConcurrentRequests = organization.MaxConcurrentRequests,
                MonthlyRequestLimit = (int?)organization.MonthlyRequestLimit,
                MonthlyTokenLimit = (int?)organization.MonthlyTokenLimit,
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
