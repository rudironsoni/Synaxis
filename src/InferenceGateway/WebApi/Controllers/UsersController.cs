// <copyright file="UsersController.cs" company="Synaxis">
// Copyright (c) Synaxis. All rights reserved.
// </copyright>

namespace Synaxis.InferenceGateway.WebApi.Controllers
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
    using Synaxis.Infrastructure.Data;

    /// <summary>
    /// Controller for managing user profiles and account settings.
    /// </summary>
    [ApiController]
    [Route("api/v1/users")]
    [Authorize]
    [EnableCors("WebApp")]
    public class UsersController : ControllerBase
    {
        private readonly SynaxisDbContext _synaxisDbContext;

        /// <summary>
        /// Initializes a new instance of the <see cref="UsersController"/> class.
        /// </summary>
        /// <param name="synaxisDbContext">The Synaxis database context.</param>
        public UsersController(SynaxisDbContext synaxisDbContext)
        {
            this._synaxisDbContext = synaxisDbContext;
        }

        /// <summary>
        /// Gets the current user's profile.
        /// </summary>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>The current user's profile.</returns>
        [HttpGet("me")]
        public async Task<IActionResult> GetMe(CancellationToken cancellationToken)
        {
            var userId = this.GetUserId();

            var user = await this._synaxisDbContext.Users
                .FirstOrDefaultAsync(u => u.Id == userId, cancellationToken)
                .ConfigureAwait(false);

            if (user == null)
            {
                return this.NotFound();
            }

            var response = new
            {
                id = user.Id,
                email = user.Email,
                firstName = user.FirstName,
                lastName = user.LastName,
                avatarUrl = user.AvatarUrl,
                timezone = user.Timezone,
                locale = user.Locale,
                role = user.Role,
                dataResidencyRegion = user.DataResidencyRegion,
                crossBorderConsentGiven = user.CrossBorderConsentGiven,
                crossBorderConsentDate = user.CrossBorderConsentDate,
                mfaEnabled = user.MfaEnabled,
                isActive = user.IsActive,
                createdAt = user.CreatedAt,
                updatedAt = user.UpdatedAt,
            };

            return this.Ok(response);
        }

        /// <summary>
        /// Updates the current user's profile.
        /// </summary>
        /// <param name="request">The update request.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>The updated user profile.</returns>
        [HttpPut("me")]
        public async Task<IActionResult> UpdateMe([FromBody] UpdateUserRequest request, CancellationToken cancellationToken)
        {
            var userId = this.GetUserId();

            var user = await this._synaxisDbContext.Users
                .FirstOrDefaultAsync(u => u.Id == userId, cancellationToken)
                .ConfigureAwait(false);

            if (user == null)
            {
                return this.NotFound();
            }

            if (!string.IsNullOrWhiteSpace(request.FirstName))
            {
                user.FirstName = request.FirstName;
            }

            if (!string.IsNullOrWhiteSpace(request.LastName))
            {
                user.LastName = request.LastName;
            }

            if (!string.IsNullOrWhiteSpace(request.Timezone))
            {
                user.Timezone = request.Timezone;
            }

            if (!string.IsNullOrWhiteSpace(request.Locale))
            {
                user.Locale = request.Locale;
            }

            user.UpdatedAt = DateTime.UtcNow;
            await this._synaxisDbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

            var response = new
            {
                id = user.Id,
                email = user.Email,
                firstName = user.FirstName,
                lastName = user.LastName,
                avatarUrl = user.AvatarUrl,
                timezone = user.Timezone,
                locale = user.Locale,
                role = user.Role,
                updatedAt = user.UpdatedAt,
            };

            return this.Ok(response);
        }

        /// <summary>
        /// Deletes the current user's account (soft delete).
        /// </summary>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>No content.</returns>
        [HttpDelete("me")]
        public async Task<IActionResult> DeleteMe(CancellationToken cancellationToken)
        {
            var userId = this.GetUserId();

            var user = await this._synaxisDbContext.Users
                .FirstOrDefaultAsync(u => u.Id == userId, cancellationToken)
                .ConfigureAwait(false);

            if (user == null)
            {
                return this.NotFound();
            }

            user.IsActive = false;
            user.UpdatedAt = DateTime.UtcNow;
            await this._synaxisDbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

            return this.NoContent();
        }

        /// <summary>
        /// Uploads an avatar for the current user.
        /// </summary>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>Avatar upload response.</returns>
        [HttpPost("me/avatar")]
        public async Task<IActionResult> UploadAvatar(CancellationToken cancellationToken)
        {
            var userId = this.GetUserId();

            var user = await this._synaxisDbContext.Users
                .FirstOrDefaultAsync(u => u.Id == userId, cancellationToken)
                .ConfigureAwait(false);

            if (user == null)
            {
                return this.NotFound();
            }

            var response = new
            {
                message = "Avatar upload placeholder - actual S3 upload to be implemented",
                userId = user.Id,
            };

            return this.Ok(response);
        }

        /// <summary>
        /// Lists all organizations the current user is a member of.
        /// </summary>
        /// <param name="page">The page number (1-based).</param>
        /// <param name="pageSize">The page size.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>A paginated list of organizations.</returns>
        [HttpGet("me/organizations")]
        public async Task<IActionResult> GetMyOrganizations([FromQuery] int page = 1, [FromQuery] int pageSize = 10, CancellationToken cancellationToken = default)
        {
            var userId = this.GetUserId();

            var query = this._synaxisDbContext.Organizations
                .Where(o => o.Teams.Any(t => t.TeamMemberships.Any(tm => tm.UserId == userId)))
                .OrderBy(o => o.Name);

            var totalCount = await query.CountAsync(cancellationToken).ConfigureAwait(false);
            var items = await query
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(o => new
                {
                    id = o.Id,
                    name = o.Name,
                    slug = o.Slug,
                    description = o.Description,
                    tier = o.Tier,
                    isActive = o.IsActive,
                    createdAt = o.CreatedAt,
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
        /// Lists all teams the current user is a member of.
        /// </summary>
        /// <param name="page">The page number (1-based).</param>
        /// <param name="pageSize">The page size.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>A paginated list of teams.</returns>
        [HttpGet("me/teams")]
        public async Task<IActionResult> GetMyTeams([FromQuery] int page = 1, [FromQuery] int pageSize = 10, CancellationToken cancellationToken = default)
        {
            var userId = this.GetUserId();

            var query = this._synaxisDbContext.Teams
                .Where(t => t.TeamMemberships.Any(tm => tm.UserId == userId))
                .OrderBy(t => t.Name);

            var totalCount = await query.CountAsync(cancellationToken).ConfigureAwait(false);
            var items = await query
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(t => new
                {
                    id = t.Id,
                    organizationId = t.OrganizationId,
                    name = t.Name,
                    slug = t.Slug,
                    description = t.Description,
                    isActive = t.IsActive,
                    createdAt = t.CreatedAt,
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
        /// Requests a GDPR data export for the current user.
        /// </summary>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>Data export request response.</returns>
        [HttpPost("me/data-export")]
        public async Task<IActionResult> RequestDataExport(CancellationToken cancellationToken)
        {
            var userId = this.GetUserId();

            var user = await this._synaxisDbContext.Users
                .FirstOrDefaultAsync(u => u.Id == userId, cancellationToken)
                .ConfigureAwait(false);

            if (user == null)
            {
                return this.NotFound();
            }

            var response = new
            {
                message = "Data export request received - actual export processing to be implemented",
                userId = user.Id,
                requestedAt = DateTime.UtcNow,
            };

            return this.Accepted(response);
        }

        /// <summary>
        /// Updates cross-border data transfer consent for the current user.
        /// </summary>
        /// <param name="request">The consent update request.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>Updated consent information.</returns>
        [HttpPut("me/cross-border-consent")]
        public async Task<IActionResult> UpdateCrossBorderConsent([FromBody] UpdateCrossBorderConsentRequest request, CancellationToken cancellationToken)
        {
            var userId = this.GetUserId();

            var validationResult = this.ValidateCrossBorderConsentRequest(request);
            if (validationResult != null)
            {
                return validationResult;
            }

            var user = await this._synaxisDbContext.Users
                .FirstOrDefaultAsync(u => u.Id == userId, cancellationToken)
                .ConfigureAwait(false);

            if (user == null)
            {
                return this.NotFound();
            }

            // Validation ensures ConsentGiven is not null
            user.CrossBorderConsentGiven = request.ConsentGiven!.Value;
            user.CrossBorderConsentDate = DateTime.UtcNow;
            user.CrossBorderConsentVersion = request.ConsentVersion ?? "v1.0";
            user.UpdatedAt = DateTime.UtcNow;

            await this._synaxisDbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

            var response = new
            {
                crossBorderConsentGiven = user.CrossBorderConsentGiven,
                crossBorderConsentDate = user.CrossBorderConsentDate,
                crossBorderConsentVersion = user.CrossBorderConsentVersion,
                updatedAt = user.UpdatedAt,
            };

            return this.Ok(response);
        }

        private Guid GetUserId()
        {
            var userIdClaim = this.User.FindFirstValue(ClaimTypes.NameIdentifier) ?? this.User.FindFirstValue("sub");
            return Guid.Parse(userIdClaim!);
        }

        private IActionResult? ValidateCrossBorderConsentRequest(UpdateCrossBorderConsentRequest request)
        {
            if (!request.ConsentGiven.HasValue)
            {
                return this.BadRequest("ConsentGiven is required");
            }

            return null;
        }
    }

    /// <summary>
    /// Request to update user profile.
    /// </summary>
    public class UpdateUserRequest
    {
        /// <summary>
        /// Gets or sets the first name.
        /// </summary>
        public string? FirstName { get; set; }

        /// <summary>
        /// Gets or sets the last name.
        /// </summary>
        public string? LastName { get; set; }

        /// <summary>
        /// Gets or sets the timezone.
        /// </summary>
        public string? Timezone { get; set; }

        /// <summary>
        /// Gets or sets the locale.
        /// </summary>
        public string? Locale { get; set; }
    }

    /// <summary>
    /// Request to update cross-border consent.
    /// </summary>
    public class UpdateCrossBorderConsentRequest
    {
        /// <summary>
        /// Gets or sets a value indicating whether consent is given.
        /// </summary>
        public bool? ConsentGiven { get; set; }

        /// <summary>
        /// Gets or sets the consent version.
        /// </summary>
        public string? ConsentVersion { get; set; }
    }
}
