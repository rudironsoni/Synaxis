// <copyright file="IInvitationService.cs" company="Synaxis">
// Copyright (c) Synaxis. All rights reserved.
// </copyright>

namespace Synaxis.Core.Contracts
{
    using System;
    using System.Threading;
    using System.Threading.Tasks;

    /// <summary>
    /// Service for managing team invitations.
    /// </summary>
    public interface IInvitationService
    {
        /// <summary>
        /// Creates a new invitation to join a team.
        /// </summary>
        /// <param name="teamId">The team ID.</param>
        /// <param name="email">The email address to invite.</param>
        /// <param name="role">The role to assign.</param>
        /// <param name="invitedByUserId">The user creating the invitation.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>The created invitation response.</returns>
        Task<InvitationResponse> CreateInvitationAsync(Guid teamId, string email, string role, Guid invitedByUserId, CancellationToken cancellationToken = default);

        /// <summary>
        /// Gets an invitation by token.
        /// </summary>
        /// <param name="token">The invitation token.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>The invitation response or null if not found.</returns>
        Task<InvitationResponse> GetInvitationAsync(string token, CancellationToken cancellationToken = default);

        /// <summary>
        /// Accepts an invitation and adds the user to the team.
        /// </summary>
        /// <param name="token">The invitation token.</param>
        /// <param name="userId">The user accepting the invitation.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>A task representing the asynchronous operation.</returns>
        Task AcceptInvitationAsync(string token, Guid userId, CancellationToken cancellationToken = default);

        /// <summary>
        /// Declines an invitation.
        /// </summary>
        /// <param name="token">The invitation token.</param>
        /// <param name="userId">The user declining the invitation.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>A task representing the asynchronous operation.</returns>
        Task DeclineInvitationAsync(string token, Guid userId, CancellationToken cancellationToken = default);

        /// <summary>
        /// Lists pending invitations for an organization with pagination.
        /// </summary>
        /// <param name="organizationId">The organization ID.</param>
        /// <param name="page">The page number (1-based).</param>
        /// <param name="pageSize">The page size.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>Paginated list of pending invitations.</returns>
        Task<InvitationListResponse> ListPendingInvitationsAsync(Guid organizationId, int page, int pageSize, CancellationToken cancellationToken = default);

        /// <summary>
        /// Cancels an invitation.
        /// </summary>
        /// <param name="invitationId">The invitation ID.</param>
        /// <param name="cancelledByUserId">The user cancelling the invitation.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>A task representing the asynchronous operation.</returns>
        Task CancelInvitationAsync(Guid invitationId, Guid cancelledByUserId, CancellationToken cancellationToken = default);
    }
}
