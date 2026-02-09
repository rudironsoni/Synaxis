// <copyright file="ITeamService.cs" company="Synaxis">
// Copyright (c) Synaxis. All rights reserved.
// </copyright>

#nullable enable

namespace Synaxis.Core.Contracts
{
    using System;
    using System.Threading;
    using System.Threading.Tasks;

    /// <summary>
    /// Service for managing teams within organizations.
    /// </summary>
    public interface ITeamService
    {
        /// <summary>
        /// Creates a new team within an organization.
        /// </summary>
        /// <param name="request">The create team request.</param>
        /// <param name="organizationId">The organization ID.</param>
        /// <param name="createdByUserId">The user creating the team.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>The created team response.</returns>
        Task<TeamResponse> CreateTeamAsync(CreateTeamRequest request, Guid organizationId, Guid createdByUserId, CancellationToken cancellationToken = default);

        /// <summary>
        /// Gets a team by ID with tenant validation.
        /// </summary>
        /// <param name="teamId">The team ID.</param>
        /// <param name="organizationId">The organization ID for tenant validation.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>The team response or null if not found.</returns>
        Task<TeamResponse?> GetTeamAsync(Guid teamId, Guid organizationId, CancellationToken cancellationToken = default);

        /// <summary>
        /// Updates an existing team.
        /// </summary>
        /// <param name="teamId">The team ID.</param>
        /// <param name="request">The update request.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>The updated team response.</returns>
        Task<TeamResponse> UpdateTeamAsync(Guid teamId, UpdateTeamRequest request, CancellationToken cancellationToken = default);

        /// <summary>
        /// Soft deletes a team.
        /// </summary>
        /// <param name="teamId">The team ID.</param>
        /// <param name="organizationId">The organization ID for tenant validation.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>A task representing the asynchronous operation.</returns>
        Task DeleteTeamAsync(Guid teamId, Guid organizationId, CancellationToken cancellationToken = default);

        /// <summary>
        /// Lists all teams in an organization with pagination.
        /// </summary>
        /// <param name="organizationId">The organization ID.</param>
        /// <param name="page">The page number (1-based).</param>
        /// <param name="pageSize">The page size.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>Paginated list of teams.</returns>
        Task<TeamListResponse> ListTeamsAsync(Guid organizationId, int page, int pageSize, CancellationToken cancellationToken = default);

        /// <summary>
        /// Invites a member to a team via email.
        /// </summary>
        /// <param name="teamId">The team ID.</param>
        /// <param name="email">The email address to invite.</param>
        /// <param name="role">The role to assign.</param>
        /// <param name="invitedByUserId">The user sending the invitation.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>A task representing the asynchronous operation.</returns>
        Task InviteMemberAsync(Guid teamId, string email, string role, Guid invitedByUserId, CancellationToken cancellationToken = default);

        /// <summary>
        /// Archives a team, marking it as inactive while preserving data.
        /// </summary>
        /// <param name="teamId">The team ID.</param>
        /// <param name="organizationId">The organization ID for tenant validation.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>A task representing the asynchronous operation.</returns>
        Task ArchiveTeamAsync(Guid teamId, Guid organizationId, CancellationToken cancellationToken = default);

        /// <summary>
        /// Restores an archived team, marking it as active again.
        /// </summary>
        /// <param name="teamId">The team ID.</param>
        /// <param name="organizationId">The organization ID for tenant validation.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>A task representing the asynchronous operation.</returns>
        Task RestoreTeamAsync(Guid teamId, Guid organizationId, CancellationToken cancellationToken = default);

        /// <summary>
        /// Validates if a team slug is unique within an organization.
        /// </summary>
        /// <param name="slug">The slug to validate.</param>
        /// <param name="organizationId">The organization ID.</param>
        /// <param name="excludeTeamId">Optional team ID to exclude from validation (for updates).</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>True if the slug is available, false otherwise.</returns>
        Task<bool> ValidateTeamSlugAsync(string slug, Guid organizationId, Guid? excludeTeamId = null, CancellationToken cancellationToken = default);

        /// <summary>
        /// Gets statistics for a team including member count and other metrics.
        /// </summary>
        /// <param name="teamId">The team ID.</param>
        /// <param name="organizationId">The organization ID for tenant validation.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>Team statistics response.</returns>
        Task<TeamStatsResponse> GetTeamStatsAsync(Guid teamId, Guid organizationId, CancellationToken cancellationToken = default);
    }
}
