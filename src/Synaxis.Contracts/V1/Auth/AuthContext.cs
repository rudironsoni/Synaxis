// <copyright file="AuthContext.cs" company="Synaxis">
// Copyright (c) Synaxis. All rights reserved.
// </copyright>

namespace Synaxis.Contracts.V1.Auth
{
    /// <summary>
    /// Represents authentication context with user ID, organization ID, and permissions.
    /// </summary>
    public sealed class AuthContext
    {
        /// <summary>
        /// Gets or initializes the unique identifier of the authenticated user.
        /// </summary>
        public string UserId { get; init; } = string.Empty;

        /// <summary>
        /// Gets or initializes the unique identifier of the user's organization.
        /// </summary>
        public string? OrganizationId { get; init; }

        /// <summary>
        /// Gets or initializes the permissions granted to the authenticated user.
        /// </summary>
        public string[] Permissions { get; init; } = System.Array.Empty<string>();
    }
}
