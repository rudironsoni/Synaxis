// <copyright file="IIdentityService.cs" company="PlaceholderCompany">
// Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>

namespace Synaxis.InferenceGateway.Application.Identity
{
    using System;
    using System.Threading;
    using System.Threading.Tasks;
    using Synaxis.InferenceGateway.Application.Identity.Models;

    /// <summary>
    /// Service for managing user identity, authentication, and organization memberships.
    /// </summary>
    public interface IIdentityService
    {
        /// <summary>
        /// Registers a new user without creating an organization.
        /// </summary>
        /// <param name="request">The registration request.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>The registration result.</returns>
        Task<RegistrationResult> RegisterUserAsync(
            RegisterRequest request,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Registers a new user and creates a new organization with the user as owner.
        /// Also creates a default group and assigns the user to it.
        /// </summary>
        /// <param name="request">The registration request with organization details.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>The registration result with organization information.</returns>
        Task<RegistrationResult> RegisterOrganizationAsync(
            RegisterRequest request,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Authenticates a user and generates JWT tokens.
        /// </summary>
        /// <param name="request">The login request.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>The authentication response with tokens.</returns>
        Task<AuthenticationResponse> LoginAsync(
            LoginRequest request,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Refreshes an access token using a refresh token.
        /// </summary>
        /// <param name="refreshToken">The refresh token.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>The authentication response with new tokens.</returns>
        Task<AuthenticationResponse> RefreshTokenAsync(
            string refreshToken,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Gets information about the current user.
        /// </summary>
        /// <param name="userId">The user ID.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>The user information.</returns>
        Task<UserInfo?> GetUserInfoAsync(
            Guid userId,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Assigns a user to an organization with a specific role.
        /// Creates a membership record.
        /// </summary>
        /// <param name="userId">The user ID.</param>
        /// <param name="organizationId">The organization ID.</param>
        /// <param name="role">The organization role (Owner, Admin, Member, Guest).</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>True if successful, otherwise false.</returns>
        Task<bool> AssignUserToOrganizationAsync(
            Guid userId,
            Guid organizationId,
            string role,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Assigns a user to a group with a specific group role.
        /// </summary>
        /// <param name="userId">The user ID.</param>
        /// <param name="groupId">The group ID.</param>
        /// <param name="groupRole">The group role (Admin, Member, Viewer).</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>True if successful, otherwise false.</returns>
        Task<bool> AssignUserToGroupAsync(
            Guid userId,
            Guid groupId,
            string groupRole,
            CancellationToken cancellationToken = default);
    }
}
