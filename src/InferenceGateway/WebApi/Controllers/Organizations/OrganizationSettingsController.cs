// <copyright file="OrganizationSettingsController.cs" company="Synaxis">
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
    using Synaxis.Infrastructure.Data;

    /// <summary>
    /// Controller for managing organization settings.
    /// </summary>
    [ApiController]
    [Route("api/v1/organizations/{organizationId}/settings")]
    [Authorize]
    [EnableCors("WebApp")]
    public class OrganizationSettingsController : ControllerBase
    {
        private readonly SynaxisDbContext _synaxisDbContext;
        private readonly IPasswordService _passwordService;

        /// <summary>
        /// Initializes a new instance of the <see cref="OrganizationSettingsController"/> class.
        /// </summary>
        /// <param name="synaxisDbContext">The Synaxis database context.</param>
        /// <param name="passwordService">The password service.</param>
        public OrganizationSettingsController(
            SynaxisDbContext synaxisDbContext,
            IPasswordService passwordService)
        {
            this._synaxisDbContext = synaxisDbContext;
            this._passwordService = passwordService;
        }

        /// <summary>
        /// Gets the settings for an organization.
        /// </summary>
        /// <param name="organizationId">The organization ID.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>The organization settings.</returns>
        [HttpGet]
        public async Task<IActionResult> GetOrganizationSettings(Guid organizationId, CancellationToken cancellationToken)
        {
            var userId = this.GetCurrentUserId();

            var organization = await this._synaxisDbContext.Organizations
                .FirstOrDefaultAsync(o => o.Id == organizationId, cancellationToken)
                .ConfigureAwait(false);

            if (organization == null)
            {
                return this.NotFound();
            }

            var isMember = await this._synaxisDbContext.TeamMemberships
                .AnyAsync(tm => tm.OrganizationId == organizationId && tm.UserId == userId, cancellationToken)
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
        /// <param name="organizationId">The organization ID.</param>
        /// <param name="request">The update settings request.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>The updated settings.</returns>
        [HttpPut]
        public async Task<IActionResult> UpdateOrganizationSettings(
            Guid organizationId,
            [FromBody] UpdateOrganizationSettingsRequest request,
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

        /// <summary>
        /// Gets the password policy for an organization.
        /// </summary>
        /// <param name="organizationId">The organization ID.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>The password policy.</returns>
        [HttpGet("password-policy")]
        public async Task<IActionResult> GetPasswordPolicy(Guid organizationId, CancellationToken cancellationToken)
        {
            var userId = this.GetCurrentUserId();

            var organization = await this._synaxisDbContext.Organizations
                .FirstOrDefaultAsync(o => o.Id == organizationId, cancellationToken)
                .ConfigureAwait(false);

            if (organization == null)
            {
                return this.NotFound();
            }

            var isMember = await this._synaxisDbContext.TeamMemberships
                .AnyAsync(tm => tm.OrganizationId == organizationId && tm.UserId == userId, cancellationToken)
                .ConfigureAwait(false);

            if (!isMember)
            {
                return this.Forbid();
            }

            var policy = await this._passwordService.GetPasswordPolicyAsync(organizationId);

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
        /// <param name="organizationId">The organization ID.</param>
        /// <param name="request">The update password policy request.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>The updated password policy.</returns>
        [HttpPut("password-policy")]
        public async Task<IActionResult> UpdatePasswordPolicy(
            Guid organizationId,
            [FromBody] UpdatePasswordPolicyRequest request,
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

            var validationResult = this.ValidatePasswordPolicyRequest(request);
            if (validationResult != null)
            {
                return validationResult;
            }

            var policy = CreatePasswordPolicyFromRequest(request);
            var updatedPolicy = await this._passwordService.UpdatePasswordPolicyAsync(organizationId, policy);
            var response = MapToPasswordPolicyResponse(updatedPolicy);
            return this.Ok(response);
        }

        private static Synaxis.Core.Models.PasswordPolicy CreatePasswordPolicyFromRequest(UpdatePasswordPolicyRequest request)
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

        private static PasswordPolicyResponse MapToPasswordPolicyResponse(Synaxis.Core.Models.PasswordPolicy policy)
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
