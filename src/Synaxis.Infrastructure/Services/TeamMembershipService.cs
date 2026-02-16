// <copyright file="TeamMembershipService.cs" company="Synaxis">
// Copyright (c) Synaxis. All rights reserved.
// </copyright>

namespace Synaxis.Infrastructure.Services
{
    using System;
    using System.Linq;
    using System.Threading;
    using System.Threading.Tasks;
    using Microsoft.EntityFrameworkCore;
    using Synaxis.Core.Contracts;
    using Synaxis.Core.Models;
    using Synaxis.Infrastructure.Data;

    /// <summary>
    /// Service for managing team memberships and roles.
    /// </summary>
    public class TeamMembershipService : ITeamMembershipService
    {
        private readonly SynaxisDbContext _context;
        private static readonly string[] ValidRoles = { "OrgAdmin", "TeamAdmin", "Member", "Viewer" };

        /// <summary>
        /// Initializes a new instance of the <see cref="TeamMembershipService"/> class.
        /// </summary>
        /// <param name="context">The database context.</param>
        public TeamMembershipService(SynaxisDbContext context)
        {
            this._context = context ?? throw new ArgumentNullException(nameof(context));
        }

        /// <inheritdoc/>
        public async Task<TeamMemberResponse> AddMemberAsync(Guid teamId, Guid userId, string role, Guid addedByUserId, CancellationToken cancellationToken = default)
        {
            ValidateRole(role);
            var (user, team) = await this.ValidateUserAndTeamAsync(userId, teamId, cancellationToken).ConfigureAwait(false);
            await this.ValidateMembershipDoesNotExistAsync(userId, teamId, cancellationToken).ConfigureAwait(false);

            var membership = this.CreateMembership(userId, teamId, team.OrganizationId, role, addedByUserId);
            this._context.TeamMemberships.Add(membership);
            await this._context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

            return this.CreateMemberResponse(membership, user, teamId, role);
        }

        private static void ValidateRole(string role)
        {
            if (!ValidRoles.Contains(role))
            {
                throw new ArgumentException($"Invalid role: {role}. Valid roles are: {string.Join(", ", ValidRoles)}", nameof(role));
            }
        }

        private async Task<(User User, Team Team)> ValidateUserAndTeamAsync(Guid userId, Guid teamId, CancellationToken cancellationToken)
        {
            var user = await this._context.Users
                .FirstOrDefaultAsync(u => u.Id == userId, cancellationToken).ConfigureAwait(false);

            if (user == null)
            {
                throw new InvalidOperationException($"User with ID {userId} not found");
            }

            var team = await this._context.Teams
                .FirstOrDefaultAsync(t => t.Id == teamId, cancellationToken).ConfigureAwait(false);

            if (team == null)
            {
                throw new InvalidOperationException($"Team with ID {teamId} not found");
            }

            if (user.OrganizationId != team.OrganizationId)
            {
                throw new InvalidOperationException("User is not in the same organization as the team");
            }

            return (User: user, Team: team);
        }

        private async Task ValidateMembershipDoesNotExistAsync(Guid userId, Guid teamId, CancellationToken cancellationToken)
        {
            var existingMembership = await this._context.TeamMemberships
                .FirstOrDefaultAsync(m => m.UserId == userId && m.TeamId == teamId, cancellationToken).ConfigureAwait(false);

            if (existingMembership != null)
            {
                throw new InvalidOperationException($"User is already a member of team {teamId}");
            }
        }

        private TeamMembership CreateMembership(Guid userId, Guid teamId, Guid organizationId, string role, Guid addedByUserId)
        {
            return new TeamMembership
            {
                Id = Guid.NewGuid(),
                UserId = userId,
                TeamId = teamId,
                OrganizationId = organizationId,
                Role = role,
                JoinedAt = DateTime.UtcNow,
                InvitedBy = addedByUserId,
            };
        }

        private TeamMemberResponse CreateMemberResponse(TeamMembership membership, User user, Guid teamId, string role)
        {
            return new TeamMemberResponse
            {
                Id = membership.Id,
                UserId = user.Id,
                UserEmail = user.Email,
                UserFullName = user.FullName,
                TeamId = teamId,
                Role = role,
                JoinedAt = membership.JoinedAt,
                InvitedBy = membership.InvitedBy,
            };
        }

        /// <inheritdoc/>
        public async Task RemoveMemberAsync(Guid teamId, Guid userId, Guid removedByUserId, CancellationToken cancellationToken = default)
        {
            var membership = await this._context.TeamMemberships
                .FirstOrDefaultAsync(m => m.UserId == userId && m.TeamId == teamId, cancellationToken)
                .ConfigureAwait(false);

            if (membership == null)
            {
                throw new InvalidOperationException($"User {userId} is not a member of team {teamId} or membership not found");
            }

            this._context.TeamMemberships.Remove(membership);
            await this._context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        }

        /// <inheritdoc/>
        public async Task<TeamMemberResponse> UpdateMemberRoleAsync(Guid teamId, Guid userId, string newRole, Guid updatedByUserId, CancellationToken cancellationToken = default)
        {
            if (!ValidRoles.Contains(newRole))
            {
                throw new ArgumentException($"Invalid role: {newRole}. Valid roles are: {string.Join(", ", ValidRoles)}", nameof(newRole));
            }

            var membership = await this._context.TeamMemberships
                .Include(tm => tm.Team)
                .FirstOrDefaultAsync(m => m.UserId == userId && m.TeamId == teamId, cancellationToken)
                .ConfigureAwait(false);

            if (membership == null)
            {
                throw new InvalidOperationException($"User {userId} is not a member of team {teamId} or membership not found");
            }

            membership.Role = newRole;
            await this._context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

            return new TeamMemberResponse
            {
                Id = membership.Id,
                UserId = userId,
                UserEmail = membership.User?.Email ?? string.Empty,
                UserFullName = membership.User?.FullName ?? string.Empty,
                TeamId = teamId,
                Role = newRole,
                JoinedAt = membership.JoinedAt,
                InvitedBy = membership.InvitedBy,
            };
        }

        /// <inheritdoc/>
        public async Task<TeamMemberListResponse> GetTeamMembersAsync(Guid teamId, int page, int pageSize, CancellationToken cancellationToken = default)
        {
            var query = this._context.TeamMemberships
                .Include(m => m.User)
                .Where(m => m.TeamId == teamId);

            var totalCount = await query.CountAsync(cancellationToken).ConfigureAwait(false);

            var members = await query
                .OrderBy(m => m.JoinedAt)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(m => new TeamMemberResponse
                {
                    Id = m.Id,
                    UserId = m.UserId,
                    UserEmail = m.User != null ? m.User.Email : string.Empty,
                    UserFullName = m.User != null ? m.User.FullName : string.Empty,
                    TeamId = m.TeamId,
                    Role = m.Role,
                    JoinedAt = m.JoinedAt,
                    InvitedBy = m.InvitedBy,
                })
                .ToListAsync(cancellationToken)
                .ConfigureAwait(false);

            return new TeamMemberListResponse
            {
                Members = members,
                TotalCount = totalCount,
                Page = page,
                PageSize = pageSize,
            };
        }

        /// <inheritdoc/>
        public async Task<TeamMemberListResponse> GetUserTeamsAsync(Guid userId, int page, int pageSize, CancellationToken cancellationToken = default)
        {
            var query = this._context.TeamMemberships
                .Include(m => m.Team)
                    .ThenInclude(t => t!.Organization)
                .Where(m => m.UserId == userId);

            var totalCount = await query.CountAsync(cancellationToken).ConfigureAwait(false);

            var teams = await query
                .OrderBy(m => m.JoinedAt)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(m => new TeamMemberResponse
                {
                    Id = m.Id,
                    UserId = m.UserId,
                    UserEmail = string.Empty,
                    TeamId = m.TeamId,
                    Role = m.Role,
                    JoinedAt = m.JoinedAt,
                    InvitedBy = m.InvitedBy,
                })
                .ToListAsync(cancellationToken)
                .ConfigureAwait(false);

            return new TeamMemberListResponse
            {
                Members = teams,
                TotalCount = totalCount,
                Page = page,
                PageSize = pageSize,
            };
        }

        /// <inheritdoc/>
        public async Task<bool> CheckPermissionAsync(Guid userId, Guid teamId, string permission, CancellationToken cancellationToken = default)
        {
            var membership = await this._context.TeamMemberships
                .FirstOrDefaultAsync(m => m.UserId == userId && m.TeamId == teamId, cancellationToken)
                .ConfigureAwait(false);

            if (membership == null)
            {
                return false;
            }

            return permission switch
            {
                "manage_team" => string.Equals(membership.Role, "OrgAdmin", StringComparison.Ordinal) || string.Equals(membership.Role, "TeamAdmin", StringComparison.Ordinal),
                "view_team" => true,
                "create_keys" => string.Equals(membership.Role, "OrgAdmin", StringComparison.Ordinal) || string.Equals(membership.Role, "TeamAdmin", StringComparison.Ordinal) || string.Equals(membership.Role, "Member", StringComparison.Ordinal),
                _ => false,
            };
        }
    }
}
