// <copyright file="TeamService.cs" company="Synaxis">
// Copyright (c) Synaxis. All rights reserved.
// </copyright>

#nullable enable

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
    /// Service for managing teams within organizations.
    /// </summary>
    public class TeamService : ITeamService
    {
        private readonly SynaxisDbContext _dbContext;
        private readonly IInvitationService _invitationService;

        /// <summary>
        /// Initializes a new instance of the <see cref="TeamService"/> class.
        /// </summary>
        /// <param name="dbContext">The database context.</param>
        /// <param name="invitationService">The invitation service.</param>
        public TeamService(SynaxisDbContext dbContext, IInvitationService invitationService)
        {
            _dbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));
            _invitationService = invitationService ?? throw new ArgumentNullException(nameof(invitationService));
        }

        /// <inheritdoc />
        public async Task<TeamResponse> CreateTeamAsync(
            CreateTeamRequest request,
            Guid organizationId,
            Guid createdByUserId,
            CancellationToken cancellationToken = default)
        {
            if (request == null)
            {
                throw new ArgumentNullException(nameof(request));
            }

            if (string.IsNullOrWhiteSpace(request.Slug))
            {
                throw new ArgumentException("Team slug is required.", nameof(request));
            }

            if (string.IsNullOrWhiteSpace(request.Name))
            {
                throw new ArgumentException("Team name is required.", nameof(request));
            }

            // Normalize slug
            var normalizedSlug = request.Slug.ToLowerInvariant().Trim();

            // Check slug uniqueness within organization
            var slugExists = await _dbContext.Teams
                .AnyAsync(t => t.OrganizationId == organizationId && t.Slug == normalizedSlug, cancellationToken);

            if (slugExists)
            {
                throw new InvalidOperationException($"Team with slug '{normalizedSlug}' already exists in this organization.");
            }

            // Check organization team limits
            var organization = await _dbContext.Organizations
                .FirstOrDefaultAsync(o => o.Id == organizationId, cancellationToken);

            if (organization == null)
            {
                throw new InvalidOperationException("Organization not found.");
            }

            if (organization.MaxTeams.HasValue)
            {
                var currentTeamCount = await _dbContext.Teams
                    .CountAsync(t => t.OrganizationId == organizationId && t.IsActive, cancellationToken);

                if (currentTeamCount >= organization.MaxTeams.Value)
                {
                    throw new InvalidOperationException($"Organization has reached the maximum limit of {organization.MaxTeams.Value} teams.");
                }
            }

            var team = new Team
            {
                Id = Guid.NewGuid(),
                OrganizationId = organizationId,
                Slug = normalizedSlug,
                Name = request.Name.Trim(),
                Description = request.Description?.Trim(),
                IsActive = true,
                MonthlyBudget = request.MonthlyBudget,
                AllowedModels = request.AllowedModels?.ToList(),
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
            };

            _dbContext.Teams.Add(team);
            await _dbContext.SaveChangesAsync(cancellationToken);

            return MapToResponse(team, 0);
        }

        /// <inheritdoc />
        public async Task<TeamResponse?> GetTeamAsync(
            Guid teamId,
            Guid organizationId,
            CancellationToken cancellationToken = default)
        {
            var team = await _dbContext.Teams
                .AsNoTracking()
                .FirstOrDefaultAsync(
                    t => t.Id == teamId && t.OrganizationId == organizationId && t.IsActive,
                    cancellationToken);

            if (team == null)
            {
                return null;
            }

            var memberCount = await _dbContext.TeamMemberships
                .CountAsync(tm => tm.TeamId == teamId, cancellationToken);

            return MapToResponse(team, memberCount);
        }

        /// <inheritdoc />
        public async Task<TeamResponse> UpdateTeamAsync(
            Guid teamId,
            UpdateTeamRequest request,
            CancellationToken cancellationToken = default)
        {
            if (request == null)
            {
                throw new ArgumentNullException(nameof(request));
            }

            var team = await _dbContext.Teams
                .FirstOrDefaultAsync(t => t.Id == teamId, cancellationToken);

            if (team == null)
            {
                throw new InvalidOperationException("Team not found.");
            }

            // Update properties if provided
            if (!string.IsNullOrWhiteSpace(request.Name))
            {
                team.Name = request.Name.Trim();
            }

            if (request.Description != null)
            {
                team.Description = string.IsNullOrWhiteSpace(request.Description) ? null : request.Description.Trim();
            }

            if (request.MonthlyBudget.HasValue)
            {
                team.MonthlyBudget = request.MonthlyBudget.Value;
            }

            if (request.AllowedModels != null)
            {
                team.AllowedModels = request.AllowedModels.ToList();
            }

            if (request.BlockedModels != null)
            {
                team.BlockedModels = request.BlockedModels.ToList();
            }

            if (request.IsActive.HasValue)
            {
                team.IsActive = request.IsActive.Value;
            }

            team.UpdatedAt = DateTime.UtcNow;

            await _dbContext.SaveChangesAsync(cancellationToken);

            var memberCount = await _dbContext.TeamMemberships
                .CountAsync(tm => tm.TeamId == teamId, cancellationToken);

            return MapToResponse(team, memberCount);
        }

        /// <inheritdoc />
        public async Task DeleteTeamAsync(
            Guid teamId,
            Guid organizationId,
            CancellationToken cancellationToken = default)
        {
            var team = await _dbContext.Teams
                .FirstOrDefaultAsync(
                    t => t.Id == teamId && t.OrganizationId == organizationId,
                    cancellationToken);

            if (team == null)
            {
                throw new InvalidOperationException("Team not found.");
            }

            // Soft delete
            team.IsActive = false;
            team.UpdatedAt = DateTime.UtcNow;

            await _dbContext.SaveChangesAsync(cancellationToken);
        }

        /// <inheritdoc />
        public async Task<TeamListResponse> ListTeamsAsync(
            Guid organizationId,
            int page,
            int pageSize,
            CancellationToken cancellationToken = default)
        {
            if (page < 1)
            {
                page = 1;
            }

            if (pageSize < 1)
            {
                pageSize = 10;
            }

            if (pageSize > 100)
            {
                pageSize = 100;
            }

            var query = _dbContext.Teams
                .AsNoTracking()
                .Where(t => t.OrganizationId == organizationId && t.IsActive);

            var totalCount = await query.CountAsync(cancellationToken);

            var teams = await query
                .OrderBy(t => t.Name)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync(cancellationToken);

            var teamIds = teams.Select(t => t.Id).ToList();
            var memberCounts = await _dbContext.TeamMemberships
                .Where(tm => teamIds.Contains(tm.TeamId))
                .GroupBy(tm => tm.TeamId)
                .Select(g => new { TeamId = g.Key, Count = g.Count() })
                .ToListAsync(cancellationToken);

            var memberCountDict = memberCounts.ToDictionary(mc => mc.TeamId, mc => mc.Count);

            return new TeamListResponse
            {
                Teams = teams.Select(t => MapToResponse(t, memberCountDict.GetValueOrDefault(t.Id, 0))).ToList(),
                TotalCount = totalCount,
                Page = page,
                PageSize = pageSize,
            };
        }

        /// <inheritdoc />
        public async Task InviteMemberAsync(
            Guid teamId,
            string email,
            string role,
            Guid invitedByUserId,
            CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(email))
            {
                throw new ArgumentException("Email is required.", nameof(email));
            }

            if (string.IsNullOrWhiteSpace(role))
            {
                throw new ArgumentException("Role is required.", nameof(role));
            }

            // Validate team exists
            var team = await _dbContext.Teams
                .FirstOrDefaultAsync(t => t.Id == teamId, cancellationToken);

            if (team == null)
            {
                throw new InvalidOperationException("Team not found.");
            }

            // Delegate to invitation service
            await _invitationService.CreateInvitationAsync(teamId, email, role, invitedByUserId, cancellationToken);
        }

        /// <inheritdoc />
        public async Task ArchiveTeamAsync(
            Guid teamId,
            Guid organizationId,
            CancellationToken cancellationToken = default)
        {
            var team = await _dbContext.Teams
                .FirstOrDefaultAsync(
                    t => t.Id == teamId && t.OrganizationId == organizationId,
                    cancellationToken)
                .ConfigureAwait(false);

            if (team == null)
            {
                throw new InvalidOperationException("Team not found.");
            }

            team.IsActive = false;
            team.UpdatedAt = DateTime.UtcNow;

            await _dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        }

        /// <inheritdoc />
        public async Task RestoreTeamAsync(
            Guid teamId,
            Guid organizationId,
            CancellationToken cancellationToken = default)
        {
            var team = await _dbContext.Teams
                .FirstOrDefaultAsync(
                    t => t.Id == teamId && t.OrganizationId == organizationId,
                    cancellationToken)
                .ConfigureAwait(false);

            if (team == null)
            {
                throw new InvalidOperationException("Team not found.");
            }

            team.IsActive = true;
            team.UpdatedAt = DateTime.UtcNow;

            await _dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        }

        /// <inheritdoc />
        public async Task<bool> ValidateTeamSlugAsync(
            string slug,
            Guid organizationId,
            Guid? excludeTeamId = null,
            CancellationToken cancellationToken = default)
        {
            var normalizedSlug = slug.ToLowerInvariant().Trim();

            var query = _dbContext.Teams
                .Where(t => t.OrganizationId == organizationId && t.Slug == normalizedSlug);

            if (excludeTeamId.HasValue)
            {
                query = query.Where(t => t.Id != excludeTeamId.Value);
            }

            var exists = await query.AnyAsync(cancellationToken).ConfigureAwait(false);

            return !exists;
        }

        /// <inheritdoc />
        public async Task<TeamStatsResponse> GetTeamStatsAsync(
            Guid teamId,
            Guid organizationId,
            CancellationToken cancellationToken = default)
        {
            var team = await _dbContext.Teams
                .AsNoTracking()
                .FirstOrDefaultAsync(
                    t => t.Id == teamId && t.OrganizationId == organizationId,
                    cancellationToken)
                .ConfigureAwait(false);

            if (team == null)
            {
                throw new InvalidOperationException("Team not found.");
            }

            var memberCount = await _dbContext.TeamMemberships
                .CountAsync(tm => tm.TeamId == teamId, cancellationToken)
                .ConfigureAwait(false);

            var activeKeyCount = await _dbContext.VirtualKeys
                .CountAsync(vk => vk.TeamId == teamId && vk.IsActive && !vk.IsRevoked, cancellationToken)
                .ConfigureAwait(false);

            // Get current month's start date
            var now = DateTime.UtcNow;
            var monthStart = new DateTime(now.Year, now.Month, 1, 0, 0, 0, DateTimeKind.Utc);

            var monthlyStats = await _dbContext.Requests
                .Where(r => r.TeamId == teamId && r.CreatedAt >= monthStart)
                .GroupBy(r => 1)
                .Select(g => new
                {
                    RequestCount = g.Count(),
                    TotalCost = g.Sum(r => r.Cost)
                })
                .FirstOrDefaultAsync(cancellationToken)
                .ConfigureAwait(false);

            var monthlyCost = monthlyStats?.TotalCost ?? 0m;
            var monthlyRequestCount = monthlyStats?.RequestCount ?? 0;

            decimal? budgetUtilization = null;
            if (team.MonthlyBudget.HasValue && team.MonthlyBudget.Value > 0)
            {
                budgetUtilization = (monthlyCost / team.MonthlyBudget.Value) * 100m;
            }

            return new TeamStatsResponse
            {
                TeamId = teamId,
                MemberCount = memberCount,
                ActiveKeyCount = activeKeyCount,
                MonthlyRequestCount = monthlyRequestCount,
                MonthlyCost = monthlyCost,
                MonthlyBudget = team.MonthlyBudget,
                BudgetUtilization = budgetUtilization
            };
        }

        private static TeamResponse MapToResponse(Team team, int memberCount)
        {
            return new TeamResponse
            {
                Id = team.Id,
                OrganizationId = team.OrganizationId,
                Slug = team.Slug,
                Name = team.Name,
                Description = team.Description,
                IsActive = team.IsActive,
                MonthlyBudget = team.MonthlyBudget,
                MemberCount = memberCount,
                CreatedAt = team.CreatedAt,
                UpdatedAt = team.UpdatedAt,
            };
        }
    }
}
