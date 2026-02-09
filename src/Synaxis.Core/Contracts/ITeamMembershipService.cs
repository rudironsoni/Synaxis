// <copyright file="ITeamMembershipService.cs" company="Synaxis">
// Copyright (c) Synaxis. All rights reserved.
// </copyright>

namespace Synaxis.Core.Contracts
{
    using System;
    using System.Threading;
    using System.Threading.Tasks;

    /// <summary>
    /// Service for managing team memberships and roles.
    /// </summary>
    public interface ITeamMembershipService
    {
        /// <summary>
        /// Adds a member to a team with a specified role.
        /// </summary>
        /// <param name="teamId">The team ID.</param>
        /// <param name="userId">The user ID to add.</param>
        /// <param name="role">The role to assign.</param>
        /// <param name="addedByUserId">The user performing the action.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>The created team member response.</returns>
        Task<TeamMemberResponse> AddMemberAsync(Guid teamId, Guid userId, string role, Guid addedByUserId, CancellationToken cancellationToken = default);

        /// <summary>
        /// Removes a member from a team.
        /// </summary>
        /// <param name="teamId">The team ID.</param>
        /// <param name="userId">The user ID to remove.</param>
        /// <param name="removedByUserId">The user performing the action.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>A task representing the asynchronous operation.</returns>
        Task RemoveMemberAsync(Guid teamId, Guid userId, Guid removedByUserId, CancellationToken cancellationToken = default);

        /// <summary>
        /// Updates a member's role in a team.
        /// </summary>
        /// <param name="teamId">The team ID.</param>
        /// <param name="userId">The user ID.</param>
        /// <param name="newRole">The new role to assign.</param>
        /// <param name="updatedByUserId">The user performing the action.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>The updated team member response.</returns>
        Task<TeamMemberResponse> UpdateMemberRoleAsync(Guid teamId, Guid userId, string newRole, Guid updatedByUserId, CancellationToken cancellationToken = default);

        /// <summary>
        /// Gets all members of a team with pagination.
        /// </summary>
        /// <param name="teamId">The team ID.</param>
        /// <param name="page">The page number (1-based).</param>
        /// <param name="pageSize">The page size.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>Paginated list of team members.</returns>
        Task<TeamMemberListResponse> GetTeamMembersAsync(Guid teamId, int page, int pageSize, CancellationToken cancellationToken = default);

        /// <summary>
        /// Gets all teams for a user with pagination.
        /// </summary>
        /// <param name="userId">The user ID.</param>
        /// <param name="page">The page number (1-based).</param>
        /// <param name="pageSize">The page size.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>Paginated list of user's teams.</returns>
        Task<TeamMemberListResponse> GetUserTeamsAsync(Guid userId, int page, int pageSize, CancellationToken cancellationToken = default);

        /// <summary>
        /// Checks if a user has a specific permission for a team.
        /// </summary>
        /// <param name="userId">The user ID.</param>
        /// <param name="teamId">The team ID.</param>
        /// <param name="permission">The permission to check.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>True if the user has the permission, false otherwise.</returns>
        Task<bool> CheckPermissionAsync(Guid userId, Guid teamId, string permission, CancellationToken cancellationToken = default);
    }
}
