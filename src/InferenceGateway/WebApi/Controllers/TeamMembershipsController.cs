// <copyright file="TeamMembershipsController.cs" company="PlaceholderCompany">
// Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>

namespace Synaxis.InferenceGateway.WebApi.Controllers
{
    using System.Security.Claims;
    using Microsoft.AspNetCore.Authorization;
    using Microsoft.AspNetCore.Cors;
    using Microsoft.AspNetCore.Mvc;
    using Microsoft.EntityFrameworkCore;
    using Synaxis.Core.Models;
    using Synaxis.InferenceGateway.Application.Security;
    using Synaxis.Infrastructure.Data;

    /// <summary>
    /// Controller for managing team memberships.
    /// </summary>
    [ApiController]
    [Route("api/v1/organizations/{orgId}/teams/{teamId}/members")]
    [Authorize]
    [EnableCors("WebApp")]
    public class TeamMembershipsController : ControllerBase
    {
        private readonly SynaxisDbContext _dbContext;
        private readonly IAuditService _auditService;

        /// <summary>
        /// Initializes a new instance of the <see cref="TeamMembershipsController"/> class.
        /// </summary>
        /// <param name="dbContext">The database context.</param>
        /// <param name="auditService">The audit service.</param>
        public TeamMembershipsController(SynaxisDbContext dbContext, IAuditService auditService)
        {
            this._dbContext = dbContext;
            this._auditService = auditService;
        }

        /// <summary>
        /// Lists members of a team.
        /// </summary>
        /// <param name="orgId">The organization ID.</param>
        /// <param name="teamId">The team ID.</param>
        /// <param name="pageSize">The page size.</param>
        /// <param name="page">The page number.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>List of team members.</returns>
        [HttpGet]
        public async Task<IActionResult> ListMembers(
            Guid orgId,
            Guid teamId,
            [FromQuery] int pageSize = 20,
            [FromQuery] int page = 0,
            CancellationToken cancellationToken = default)
        {
            var userId = this.GetUserId();

            var user = await this._dbContext.Users
                .FirstOrDefaultAsync(u => u.Id == userId && u.OrganizationId == orgId, cancellationToken)
                .ConfigureAwait(false);

            var isOrgAdmin = user != null && (string.Equals(user.Role, "admin", StringComparison.OrdinalIgnoreCase) || string.Equals(user.Role, "owner", StringComparison.OrdinalIgnoreCase));

            var membership = await this._dbContext.TeamMemberships
                .FirstOrDefaultAsync(m => m.UserId == userId && m.TeamId == teamId, cancellationToken)
                .ConfigureAwait(false);

            if (membership == null && !isOrgAdmin)
            {
                return this.Forbid();
            }

            var team = await this._dbContext.Teams
                .FirstOrDefaultAsync(g => g.Id == teamId && g.OrganizationId == orgId, cancellationToken)
                .ConfigureAwait(false);

            if (team == null)
            {
                return this.NotFound("Team not found");
            }

            var query = this._dbContext.TeamMemberships
                .Where(m => m.TeamId == teamId)
                .Include(m => m.User);

            var members = await query
                .Where(m => m.User != null)
                .OrderBy(m => m.User!.Email)
                .Skip(page * pageSize)
                .Take(pageSize)
                .Select(m => new
                {
                    userId = m.UserId,
                    email = m.User!.Email,
                    firstName = m.User.FirstName,
                    lastName = m.User.LastName,
                    role = m.Role,
                    joinedAt = m.JoinedAt,
                })
                .ToListAsync(cancellationToken)
                .ConfigureAwait(false);

            var total = await query.CountAsync(cancellationToken).ConfigureAwait(false);

            return this.Ok(new
            {
                members,
                total,
                page,
                pageSize,
            });
        }

        /// <summary>
        /// Gets a specific team member.
        /// </summary>
        /// <param name="orgId">The organization ID.</param>
        /// <param name="teamId">The team ID.</param>
        /// <param name="memberId">The member ID.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>Member details.</returns>
        [HttpGet("{memberId}")]
        public async Task<IActionResult> GetMember(
            Guid orgId,
            Guid teamId,
            Guid memberId,
            CancellationToken cancellationToken)
        {
            var userId = this.GetUserId();

            var user = await this._dbContext.Users
                .FirstOrDefaultAsync(u => u.Id == userId && u.OrganizationId == orgId, cancellationToken)
                .ConfigureAwait(false);

            var isOrgAdmin = user != null && (string.Equals(user.Role, "admin", StringComparison.OrdinalIgnoreCase) || string.Equals(user.Role, "owner", StringComparison.OrdinalIgnoreCase));

            var callerMembership = await this._dbContext.TeamMemberships
                .FirstOrDefaultAsync(m => m.UserId == userId && m.TeamId == teamId, cancellationToken)
                .ConfigureAwait(false);

            if (callerMembership == null && !isOrgAdmin)
            {
                return this.Forbid();
            }

            var membership = await this._dbContext.TeamMemberships
                .Include(m => m.User)
                .FirstOrDefaultAsync(m => m.UserId == memberId && m.TeamId == teamId && m.OrganizationId == orgId, cancellationToken)
                .ConfigureAwait(false);

            if (membership == null)
            {
                return this.NotFound("Member not found in team");
            }

            return this.Ok(new
            {
                userId = membership.UserId,
                email = membership.User?.Email,
                firstName = membership.User?.FirstName,
                lastName = membership.User?.LastName,
                role = membership.Role,
                joinedAt = membership.JoinedAt,
            });
        }

        /// <summary>
        /// Updates a member's role in a team.
        /// </summary>
        /// <param name="orgId">The organization ID.</param>
        /// <param name="teamId">The team ID.</param>
        /// <param name="memberId">The user ID to update.</param>
        /// <param name="request">The update role request.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>OK response.</returns>
        [HttpPut("{memberId}/role")]
        public async Task<IActionResult> UpdateMemberRole(
            Guid orgId,
            Guid teamId,
            Guid memberId,
            [FromBody] UpdateMemberRoleRequest request,
            CancellationToken cancellationToken)
        {
            var userId = this.GetUserId();

            var hasPermission = await this.CheckTeamAdminPermissionAsync(userId, orgId, teamId, cancellationToken).ConfigureAwait(false);
            if (!hasPermission)
            {
                return this.Forbid();
            }

            var validRole = ValidateRole(request.Role);
            if (validRole == null)
            {
                return this.BadRequest("Role must be 'admin' or 'member'");
            }

            var team = await this._dbContext.Teams
                .FirstOrDefaultAsync(g => g.Id == teamId && g.OrganizationId == orgId, cancellationToken)
                .ConfigureAwait(false);

            if (team == null)
            {
                return this.NotFound("Team not found");
            }

            var membership = await this._dbContext.TeamMemberships
                .FirstOrDefaultAsync(m => m.UserId == memberId && m.TeamId == teamId, cancellationToken)
                .ConfigureAwait(false);

            if (membership == null)
            {
                return this.NotFound("Member not found in team");
            }

            membership.Role = validRole;

            await this._dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

            await this._auditService.LogAsync(
                orgId,
                userId,
                "UpdateTeamMemberRole",
                new { TeamId = teamId, MemberId = memberId, NewRole = validRole },
                cancellationToken).ConfigureAwait(false);

            return this.Ok(new
            {
                userId = membership.UserId,
                role = membership.Role,
                joinedAt = membership.JoinedAt,
            });
        }

        /// <summary>
        /// Removes a member from a team.
        /// </summary>
        /// <param name="orgId">The organization ID.</param>
        /// <param name="teamId">The team ID.</param>
        /// <param name="memberId">The user ID to remove.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>No content response.</returns>
        [HttpDelete("{memberId}")]
        public async Task<IActionResult> RemoveMember(
            Guid orgId,
            Guid teamId,
            Guid memberId,
            CancellationToken cancellationToken)
        {
            var userId = this.GetUserId();

            var isSelfRemoval = userId == memberId;
            if (!isSelfRemoval)
            {
                var hasPermission = await this.CheckTeamAdminPermissionAsync(userId, orgId, teamId, cancellationToken).ConfigureAwait(false);
                if (!hasPermission)
                {
                    return this.Forbid();
                }
            }

            var team = await this._dbContext.Teams
                .FirstOrDefaultAsync(g => g.Id == teamId && g.OrganizationId == orgId, cancellationToken)
                .ConfigureAwait(false);

            if (team == null)
            {
                return this.NotFound("Team not found");
            }

            var membership = await this._dbContext.TeamMemberships
                .FirstOrDefaultAsync(m => m.UserId == memberId && m.TeamId == teamId, cancellationToken)
                .ConfigureAwait(false);

            if (membership == null)
            {
                return this.NotFound("Member not found in team");
            }

            this._dbContext.TeamMemberships.Remove(membership);
            await this._dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

            await this._auditService.LogAsync(
                orgId,
                userId,
                "RemoveTeamMember",
                new { TeamId = teamId, MemberId = memberId },
                cancellationToken).ConfigureAwait(false);

            return this.NoContent();
        }

        private async Task<bool> CheckTeamAdminPermissionAsync(
            Guid userId,
            Guid orgId,
            Guid teamId,
            CancellationToken cancellationToken)
        {
            var teamMembership = await this._dbContext.TeamMemberships
                .FirstOrDefaultAsync(m => m.UserId == userId && m.TeamId == teamId, cancellationToken)
                .ConfigureAwait(false);

            var user = await this._dbContext.Users
                .FirstOrDefaultAsync(u => u.Id == userId && u.OrganizationId == orgId, cancellationToken)
                .ConfigureAwait(false);

            var isTeamAdmin = string.Equals(teamMembership?.Role, "TeamAdmin", StringComparison.Ordinal);
            var isOrgAdmin = user != null && (string.Equals(user.Role, "admin", StringComparison.OrdinalIgnoreCase) || string.Equals(user.Role, "owner", StringComparison.OrdinalIgnoreCase));

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
    }

    /// <summary>
    /// Request to update a member's role.
    /// </summary>
    public class UpdateMemberRoleRequest
    {
        /// <summary>
        /// Gets or sets the new role for the member.
        /// </summary>
        public required string Role { get; set; }
    }
}
