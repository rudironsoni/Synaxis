// <copyright file="TeamsController.cs" company="PlaceholderCompany">
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
    /// Controller for managing teams (groups) within organizations.
    /// </summary>
    [ApiController]
    [Route("api/v1/organizations/{orgId}/teams")]
    [Authorize]
    [EnableCors("WebApp")]
    public class TeamsController : ControllerBase
    {
        private readonly SynaxisDbContext _dbContext;
        private readonly IAuditService _auditService;

        /// <summary>
        /// Initializes a new instance of the <see cref="TeamsController"/> class.
        /// </summary>
        /// <param name="dbContext">The database context.</param>
        /// <param name="auditService">The audit service.</param>
        public TeamsController(SynaxisDbContext dbContext, IAuditService auditService)
        {
            this._dbContext = dbContext;
            this._auditService = auditService;
        }

        /// <summary>
        /// Creates a new team in an organization.
        /// </summary>
        /// <param name="orgId">The organization ID.</param>
        /// <param name="request">The create team request.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>The created team.</returns>
        [HttpPost]
        public async Task<IActionResult> CreateTeam(
            Guid orgId,
            [FromBody] CreateTeamRequest request,
            CancellationToken cancellationToken)
        {
            var userId = this.GetUserId();

            var validationResult = await this.ValidateTeamCreationAsync(userId, orgId, request, cancellationToken).ConfigureAwait(false);
            if (validationResult != null)
            {
                return validationResult;
            }

            var team = this.CreateTeamEntity(orgId, request);
            this._dbContext.Teams.Add(team);

            var teamMembership = this.CreateTeamMembership(userId, team.Id, orgId);
            this._dbContext.TeamMemberships.Add(teamMembership);

            await this._dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

            await this._auditService.LogAsync(
                orgId,
                userId,
                "CreateTeam",
                new { TeamId = team.Id, Name = team.Name },
                cancellationToken).ConfigureAwait(false);

            return this.CreatedAtAction(
                nameof(this.GetTeam),
                new { orgId, teamId = team.Id },
                new
                {
                    id = team.Id,
                    name = team.Name,
                    description = team.Description,
                    slug = team.Slug,
                    isActive = team.IsActive,
                    createdAt = team.CreatedAt,
                });
        }

        private async Task<IActionResult?> ValidateTeamCreationAsync(
            Guid userId,
            Guid orgId,
            CreateTeamRequest request,
            CancellationToken cancellationToken)
        {
            var user = await this._dbContext.Users
                .FirstOrDefaultAsync(u => u.Id == userId && u.OrganizationId == orgId, cancellationToken)
                .ConfigureAwait(false);

            if (user == null)
            {
                return this.Forbid();
            }

            var org = await this._dbContext.Organizations
                .FirstOrDefaultAsync(o => o.Id == orgId, cancellationToken)
                .ConfigureAwait(false);

            if (org == null)
            {
                return this.NotFound("Organization not found");
            }

            var existingTeam = await this._dbContext.Teams
                .FirstOrDefaultAsync(g => g.OrganizationId == orgId && g.Name == request.Name, cancellationToken)
                .ConfigureAwait(false);

            if (existingTeam != null)
            {
                return this.BadRequest("A team with this name already exists in the organization");
            }

            return null;
        }

        private Team CreateTeamEntity(Guid orgId, CreateTeamRequest request)
        {
            return new Team
            {
                Id = Guid.NewGuid(),
                OrganizationId = orgId,
                Name = request.Name,
                Description = request.Description ?? string.Empty,
                Slug = GenerateSlug(request.Name),
                IsActive = true,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
            };
        }

        private TeamMembership CreateTeamMembership(Guid userId, Guid teamId, Guid orgId)
        {
            return new TeamMembership
            {
                Id = Guid.NewGuid(),
                UserId = userId,
                TeamId = teamId,
                OrganizationId = orgId,
                Role = "TeamAdmin",
                JoinedAt = DateTime.UtcNow,
            };
        }

        /// <summary>
        /// Lists teams in an organization.
        /// </summary>
        /// <param name="orgId">The organization ID.</param>
        /// <param name="pageSize">The page size.</param>
        /// <param name="page">The page number.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>List of teams.</returns>
        [HttpGet]
        public async Task<IActionResult> ListTeams(
            Guid orgId,
            [FromQuery] int pageSize = 20,
            [FromQuery] int page = 0,
            CancellationToken cancellationToken = default)
        {
            var userId = this.GetUserId();

            // Check if user is a member of the organization
            var user = await this._dbContext.Users
                .FirstOrDefaultAsync(u => u.Id == userId && u.OrganizationId == orgId, cancellationToken)
                .ConfigureAwait(false);

            if (user == null)
            {
                return this.Forbid();
            }

            var query = this._dbContext.Teams.Where(g => g.OrganizationId == orgId);

            var teams = await query
                .OrderBy(g => g.Name)
                .Skip(page * pageSize)
                .Take(pageSize)
                .Select(g => new
                {
                    id = g.Id,
                    name = g.Name,
                    description = g.Description,
                    slug = g.Slug,
                    isActive = g.IsActive,
                    memberCount = g.TeamMemberships.Count,
                })
                .ToListAsync(cancellationToken)
                .ConfigureAwait(false);

            var total = await query.CountAsync(cancellationToken).ConfigureAwait(false);

            return this.Ok(new
            {
                teams,
                total,
                page,
                pageSize,
            });
        }

        /// <summary>
        /// Gets details of a specific team.
        /// </summary>
        /// <param name="orgId">The organization ID.</param>
        /// <param name="teamId">The team ID.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>Team details.</returns>
        [HttpGet("{teamId}")]
        public async Task<IActionResult> GetTeam(
            Guid orgId,
            Guid teamId,
            CancellationToken cancellationToken)
        {
            var userId = this.GetUserId();

            // Check if user is a member of the team
            var teamMembership = await this._dbContext.TeamMemberships
                .FirstOrDefaultAsync(m => m.UserId == userId && m.TeamId == teamId, cancellationToken)
                .ConfigureAwait(false);

            if (teamMembership == null)
            {
                return this.Forbid();
            }

            var team = await this._dbContext.Teams
                .Where(g => g.Id == teamId && g.OrganizationId == orgId)
                .Select(g => new
                {
                    id = g.Id,
                    name = g.Name,
                    description = g.Description,
                    slug = g.Slug,
                    isActive = g.IsActive,
                    memberCount = g.TeamMemberships.Count,
                    createdAt = g.CreatedAt,
                })
                .FirstOrDefaultAsync(cancellationToken)
                .ConfigureAwait(false);

            if (team == null)
            {
                return this.NotFound("Team not found");
            }

            return this.Ok(team);
        }

        /// <summary>
        /// Updates a team.
        /// </summary>
        /// <param name="orgId">The organization ID.</param>
        /// <param name="teamId">The team ID.</param>
        /// <param name="request">The update request.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>Updated team details.</returns>
        [HttpPut("{teamId}")]
        public async Task<IActionResult> UpdateTeam(
            Guid orgId,
            Guid teamId,
            [FromBody] UpdateTeamRequest request,
            CancellationToken cancellationToken)
        {
            var userId = this.GetUserId();

            var hasPermission = await this.CheckTeamAdminPermissionAsync(userId, orgId, teamId, cancellationToken).ConfigureAwait(false);
            if (!hasPermission)
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

            ApplyTeamUpdates(team, request);

            await this._dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

            await this._auditService.LogAsync(
                orgId,
                userId,
                "UpdateTeam",
                new { TeamId = team.Id, Name = team.Name },
                cancellationToken).ConfigureAwait(false);

            return this.Ok(new
            {
                id = team.Id,
                name = team.Name,
                description = team.Description,
                slug = team.Slug,
                isActive = team.IsActive,
                updatedAt = team.UpdatedAt,
            });
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

        private static void ApplyTeamUpdates(Team team, UpdateTeamRequest request)
        {
            if (request.Name != null)
            {
                team.Name = request.Name;
            }

            if (request.Description != null)
            {
                team.Description = request.Description;
            }

            team.UpdatedAt = DateTime.UtcNow;
        }

        /// <summary>
        /// Soft deletes a team.
        /// </summary>
        /// <param name="orgId">The organization ID.</param>
        /// <param name="teamId">The team ID.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>No content.</returns>
        [HttpDelete("{teamId}")]
        public async Task<IActionResult> DeleteTeam(
            Guid orgId,
            Guid teamId,
            CancellationToken cancellationToken)
        {
            var userId = this.GetUserId();

            var hasPermission = await this.CheckTeamAdminPermissionAsync(userId, orgId, teamId, cancellationToken).ConfigureAwait(false);
            if (!hasPermission)
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

            team.IsActive = false;
            team.UpdatedAt = DateTime.UtcNow;

            await this._dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

            await this._auditService.LogAsync(
                orgId,
                userId,
                "DeleteTeam",
                new { TeamId = team.Id, Name = team.Name },
                cancellationToken).ConfigureAwait(false);

            return this.NoContent();
        }

        private Guid GetUserId()
        {
            var userIdClaim = this.User.FindFirstValue(ClaimTypes.NameIdentifier) ?? this.User.FindFirstValue("sub");
            return Guid.Parse(userIdClaim!);
        }

        private static string GenerateSlug(string name)
        {
            return name.ToLowerInvariant()
                .Replace(" ", "-")
                .Replace("_", "-")
                + "-" + Guid.NewGuid().ToString("N")[..6];
        }
    }

    /// <summary>
    /// Request to create a new team.
    /// </summary>
    public class CreateTeamRequest
    {
        /// <summary>
        /// Gets or sets the team name.
        /// </summary>
        public required string Name { get; set; }

        /// <summary>
        /// Gets or sets the team description.
        /// </summary>
        public string? Description { get; set; }
    }

    /// <summary>
    /// Request to update a team.
    /// </summary>
    public class UpdateTeamRequest
    {
        /// <summary>
        /// Gets or sets the team name.
        /// </summary>
        public string? Name { get; set; }

        /// <summary>
        /// Gets or sets the team description.
        /// </summary>
        public string? Description { get; set; }
    }

    /// <summary>
    /// Request to add a member to a team.
    /// </summary>
    public class AddMemberRequest
    {
        /// <summary>
        /// Gets or sets the user ID to add.
        /// </summary>
        public required Guid UserId { get; set; }

        /// <summary>
        /// Gets or sets the role for the member.
        /// </summary>
        public required string Role { get; set; }
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
