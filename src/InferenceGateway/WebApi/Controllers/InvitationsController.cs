// <copyright file="InvitationsController.cs" company="Synaxis">
// Copyright (c) Synaxis. All rights reserved.
// </copyright>

namespace Synaxis.InferenceGateway.WebApi.Controllers
{
    using System;
    using System.ComponentModel.DataAnnotations;
    using System.Security.Claims;
    using System.Threading;
    using System.Threading.Tasks;
    using Microsoft.AspNetCore.Authorization;
    using Microsoft.AspNetCore.Cors;
    using Microsoft.AspNetCore.Mvc;
    using Microsoft.EntityFrameworkCore;
    using Synaxis.Core.Models;
    using Synaxis.Infrastructure.Data;

    /// <summary>
    /// Controller for managing team invitations.
    /// </summary>
    [ApiController]
    [Route("api/v1")]
    [EnableCors("WebApp")]
    public class InvitationsController : ControllerBase
    {
        private readonly SynaxisDbContext _dbContext;

        /// <summary>
        /// Initializes a new instance of the <see cref="InvitationsController"/> class.
        /// </summary>
        /// <param name="dbContext">The database context.</param>
        public InvitationsController(SynaxisDbContext dbContext)
        {
            this._dbContext = dbContext;
        }

        /// <summary>
        /// Creates a new team invitation.
        /// </summary>
        /// <param name="request">The create invitation request.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>The created invitation.</returns>
        [HttpPost("invitations")]
        [Authorize]
        public async Task<IActionResult> CreateInvitation(
            [FromBody] CreateInvitationRequest request,
            CancellationToken cancellationToken)
        {
            var userId = this.GetUserId();

            var validationResult = await this.ValidateInvitationCreationAsync(userId, request, cancellationToken).ConfigureAwait(false);
            if (validationResult != null)
            {
                return validationResult;
            }

            var existingInvitation = await this._dbContext.Invitations
                .FirstOrDefaultAsync(
                    i => i.TeamId == request.TeamId && i.Email == request.Email && i.Status == "pending",
                    cancellationToken)
                .ConfigureAwait(false);

            if (existingInvitation != null)
            {
                return this.BadRequest("An invitation for this email already exists for this team");
            }

            var invitation = this.CreateInvitationEntity(request, userId);
            this._dbContext.Invitations.Add(invitation);
            await this._dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

            return this.CreatedAtAction(
                nameof(this.GetInvitationByToken),
                new { token = invitation.Token },
                new
                {
                    id = invitation.Id,
                    token = invitation.Token,
                    email = invitation.Email,
                    role = invitation.Role,
                    status = invitation.Status,
                    expiresAt = invitation.ExpiresAt,
                    createdAt = invitation.CreatedAt,
                });
        }

        private async Task<IActionResult?> ValidateInvitationCreationAsync(
            Guid userId,
            CreateInvitationRequest request,
            CancellationToken cancellationToken)
        {
            var teamMembership = await this._dbContext.TeamMemberships
                .FirstOrDefaultAsync(tm => tm.UserId == userId && tm.TeamId == request.TeamId, cancellationToken)
                .ConfigureAwait(false);

            var isTeamAdmin = teamMembership != null && string.Equals(teamMembership.Role, "TeamAdmin", StringComparison.Ordinal);
            var isOrgAdmin = await this._dbContext.TeamMemberships
                .AnyAsync(tm => tm.UserId == userId && tm.OrganizationId == request.OrganizationId && string.Equals(tm.Role, "OrgAdmin"), cancellationToken)
                .ConfigureAwait(false);

            if (!isTeamAdmin && !isOrgAdmin)
            {
                return this.Forbid();
            }

            var team = await this._dbContext.Teams
                .FirstOrDefaultAsync(t => t.Id == request.TeamId && t.OrganizationId == request.OrganizationId, cancellationToken)
                .ConfigureAwait(false);

            if (team == null)
            {
                return this.NotFound("Team not found");
            }

            return null;
        }

        private Invitation CreateInvitationEntity(CreateInvitationRequest request, Guid userId)
        {
            var validRole = ValidateRole(request.Role) ?? "Member";

            return new Invitation
            {
                Id = Guid.NewGuid(),
                OrganizationId = request.OrganizationId,
                TeamId = request.TeamId,
                Email = request.Email,
                Role = validRole,
                Token = GenerateSecureToken(),
                InvitedBy = userId,
                Status = "pending",
                CreatedAt = DateTime.UtcNow,
                ExpiresAt = DateTime.UtcNow.AddDays(7),
            };
        }

        /// <summary>
        /// Gets invitation details by token.
        /// </summary>
        /// <param name="token">The invitation token.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>Invitation details.</returns>
        [HttpGet("invitations/{token}")]
        [AllowAnonymous]
        public async Task<IActionResult> GetInvitationByToken(
            string token,
            CancellationToken cancellationToken)
        {
            var invitation = await this._dbContext.Invitations
                .Include(i => i.Organization)
                .Include(i => i.Team)
                .Include(i => i.Inviter)
                .FirstOrDefaultAsync(i => i.Token == token, cancellationToken)
                .ConfigureAwait(false);

            if (invitation == null || invitation.IsExpired)
            {
                return this.NotFound("Invitation not found or expired");
            }

            return this.Ok(new
            {
                id = invitation.Id,
                email = invitation.Email,
                role = invitation.Role,
                status = invitation.Status,
                organizationName = invitation.Organization?.Name ?? "Unknown",
                teamName = invitation.Team?.Name ?? "Unknown",
                inviterName = invitation.Inviter != null ? $"{invitation.Inviter.FirstName} {invitation.Inviter.LastName}".Trim() : "Unknown",
                expiresAt = invitation.ExpiresAt,
                createdAt = invitation.CreatedAt,
            });
        }

        /// <summary>
        /// Accepts an invitation.
        /// </summary>
        /// <param name="token">The invitation token.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>The accepted invitation.</returns>
        [HttpPost("invitations/{token}/accept")]
        [Authorize]
        public async Task<IActionResult> AcceptInvitation(
            string token,
            CancellationToken cancellationToken)
        {
            var userId = this.GetUserId();

            var invitation = await this._dbContext.Invitations
                .FirstOrDefaultAsync(i => i.Token == token, cancellationToken)
                .ConfigureAwait(false);

            var validationResult = this.ValidateInvitationForAcceptance(invitation);
            if (validationResult != null)
            {
                return validationResult;
            }

            var existingMembership = await this._dbContext.TeamMemberships
                .FirstOrDefaultAsync(tm => tm.UserId == userId && tm.TeamId == invitation!.TeamId, cancellationToken)
                .ConfigureAwait(false);

            if (existingMembership != null)
            {
                return this.BadRequest("You are already a member of this team");
            }

            await this.ProcessInvitationAcceptanceAsync(invitation!, userId, cancellationToken).ConfigureAwait(false);

            return this.Ok(new
            {
                id = invitation!.Id,
                status = invitation.Status,
                acceptedAt = invitation.AcceptedAt,
            });
        }

        private IActionResult? ValidateInvitationForAcceptance(Invitation? invitation)
        {
            if (invitation == null)
            {
                return this.NotFound("Invitation not found");
            }

            if (invitation.IsExpired)
            {
                return this.BadRequest("Invitation has expired");
            }

            if (!string.Equals(invitation.Status, "pending", StringComparison.Ordinal))
            {
                return this.BadRequest($"Invitation has already been {invitation.Status}");
            }

            return null;
        }

        private Task ProcessInvitationAcceptanceAsync(
            Invitation invitation,
            Guid userId,
            CancellationToken cancellationToken)
        {
            var membership = new TeamMembership
            {
                Id = Guid.NewGuid(),
                UserId = userId,
                TeamId = invitation.TeamId,
                OrganizationId = invitation.OrganizationId,
                Role = invitation.Role,
                JoinedAt = DateTime.UtcNow,
                InvitedBy = invitation.InvitedBy,
            };

            this._dbContext.TeamMemberships.Add(membership);

            invitation.Status = "accepted";
            invitation.AcceptedAt = DateTime.UtcNow;
            invitation.AcceptedBy = userId;

            return this._dbContext.SaveChangesAsync(cancellationToken);
        }

        /// <summary>
        /// Declines an invitation.
        /// </summary>
        /// <param name="token">The invitation token.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>The declined invitation.</returns>
        [HttpPost("invitations/{token}/decline")]
        [Authorize]
        public async Task<IActionResult> DeclineInvitation(
            string token,
            CancellationToken cancellationToken)
        {
            var userId = this.GetUserId();

            var invitation = await this._dbContext.Invitations
                .FirstOrDefaultAsync(i => i.Token == token, cancellationToken)
                .ConfigureAwait(false);

            if (invitation == null)
            {
                return this.NotFound("Invitation not found");
            }

            if (!string.Equals(invitation.Status, "pending", StringComparison.Ordinal))
            {
                return this.BadRequest($"Invitation has already been {invitation.Status}");
            }

            invitation.Status = "declined";
            invitation.DeclinedAt = DateTime.UtcNow;
            invitation.DeclinedBy = userId;

            await this._dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

            return this.Ok(new
            {
                id = invitation.Id,
                status = invitation.Status,
                declinedAt = invitation.DeclinedAt,
            });
        }

        /// <summary>
        /// Lists pending invitations for an organization.
        /// </summary>
        /// <param name="orgId">The organization ID.</param>
        /// <param name="page">The page number.</param>
        /// <param name="pageSize">The page size.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>List of invitations.</returns>
        [HttpGet("organizations/{orgId}/invitations")]
        [Authorize]
        public async Task<IActionResult> ListInvitations(
            Guid orgId,
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 10,
            CancellationToken cancellationToken = default)
        {
            var userId = this.GetUserId();

            var isMember = await this._dbContext.TeamMemberships
                .AnyAsync(tm => tm.UserId == userId && tm.OrganizationId == orgId, cancellationToken)
                .ConfigureAwait(false);

            if (!isMember)
            {
                return this.Forbid();
            }

            var query = this._dbContext.Invitations
                .Include(i => i.Team)
                .Include(i => i.Inviter)
                .Where(i => i.OrganizationId == orgId && i.Status == "pending");

            var totalCount = await query.CountAsync(cancellationToken).ConfigureAwait(false);

            var invitations = await query
                .OrderByDescending(i => i.CreatedAt)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(i => new
                {
                    id = i.Id,
                    email = i.Email,
                    role = i.Role,
                    status = i.Status,
                    teamId = i.TeamId,
                    teamName = i.Team != null ? i.Team.Name : "Unknown",
                    invitedBy = i.InvitedBy,
                    inviterName = i.Inviter != null ? $"{i.Inviter.FirstName} {i.Inviter.LastName}".Trim() : "Unknown",
                    createdAt = i.CreatedAt,
                    expiresAt = i.ExpiresAt,
                })
                .ToListAsync(cancellationToken)
                .ConfigureAwait(false);

            return this.Ok(new
            {
                items = invitations,
                page,
                pageSize,
                totalCount,
                totalPages = (int)Math.Ceiling((double)totalCount / pageSize),
            });
        }

        /// <summary>
        /// Cancels an invitation.
        /// </summary>
        /// <param name="id">The invitation ID.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>No content.</returns>
        [HttpDelete("invitations/{id}")]
        [Authorize]
        public async Task<IActionResult> CancelInvitation(
            Guid id,
            CancellationToken cancellationToken)
        {
            var userId = this.GetUserId();

            var invitation = await this._dbContext.Invitations
                .FirstOrDefaultAsync(i => i.Id == id, cancellationToken)
                .ConfigureAwait(false);

            if (invitation == null)
            {
                return this.NotFound("Invitation not found");
            }

            var hasPermission = await this.CheckCancelPermissionAsync(userId, invitation, cancellationToken).ConfigureAwait(false);
            if (!hasPermission)
            {
                return this.Forbid();
            }

            invitation.Status = "cancelled";
            invitation.CancelledAt = DateTime.UtcNow;
            invitation.CancelledBy = userId;

            await this._dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

            return this.NoContent();
        }

        private async Task<bool> CheckCancelPermissionAsync(
            Guid userId,
            Invitation invitation,
            CancellationToken cancellationToken)
        {
            if (invitation.InvitedBy == userId)
            {
                return true;
            }

            var teamMembership = await this._dbContext.TeamMemberships
                .FirstOrDefaultAsync(tm => tm.UserId == userId && tm.TeamId == invitation.TeamId, cancellationToken)
                .ConfigureAwait(false);

            var isTeamAdmin = teamMembership != null && string.Equals(teamMembership.Role, "TeamAdmin", StringComparison.Ordinal);
            var isOrgAdmin = await this._dbContext.TeamMemberships
                .AnyAsync(tm => tm.UserId == userId && tm.OrganizationId == invitation.OrganizationId && string.Equals(tm.Role, "OrgAdmin"), cancellationToken)
                .ConfigureAwait(false);

            return isTeamAdmin || isOrgAdmin;
        }

        private Guid GetUserId()
        {
            var userIdClaim = this.User.FindFirstValue(ClaimTypes.NameIdentifier) ?? this.User.FindFirstValue("sub");
            return Guid.Parse(userIdClaim!);
        }

        private static string? ValidateRole(string role)
        {
            var normalized = role.Trim().ToLowerInvariant();
            return normalized switch
            {
                "admin" or "teamadmin" => "TeamAdmin",
                "orgadmin" => "OrgAdmin",
                "member" => "Member",
                "viewer" => "Viewer",
                _ => null,
            };
        }

        private static string GenerateSecureToken()
        {
            return Guid.NewGuid().ToString("N") + Guid.NewGuid().ToString("N");
        }
    }

    /// <summary>
    /// Request to create a new invitation.
    /// </summary>
    public class CreateInvitationRequest
    {
        /// <summary>
        /// Gets or sets the organization ID.
        /// </summary>
        public required Guid OrganizationId { get; set; }

        /// <summary>
        /// Gets or sets the team ID.
        /// </summary>
        public required Guid TeamId { get; set; }

        /// <summary>
        /// Gets or sets the email address.
        /// </summary>
        [Required]
        [EmailAddress]
        public required string Email { get; set; }

        /// <summary>
        /// Gets or sets the role.
        /// </summary>
        [Required]
        public string Role { get; set; } = "member";
    }
}
