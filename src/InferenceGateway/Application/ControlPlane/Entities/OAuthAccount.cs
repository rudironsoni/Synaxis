// <copyright file="OAuthAccount.cs" company="PlaceholderCompany">
// Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>

namespace Synaxis.InferenceGateway.Application.ControlPlane.Entities
{
    using System;

    /// <summary>
    /// Represents an OAuth account for external authentication.
    /// </summary>
    public sealed class OAuthAccount
    {
        /// <summary>
        /// Gets or sets the OAuth account ID.
        /// </summary>
        public Guid Id { get; set; }

        /// <summary>
        /// Gets or sets the tenant ID.
        /// </summary>
        public Guid TenantId { get; set; }

        /// <summary>
        /// Gets or sets the OAuth provider.
        /// </summary>
        public string Provider { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the encrypted access token.
        /// </summary>
        public byte[] AccessTokenEncrypted { get; set; } = Array.Empty<byte>();

        /// <summary>
        /// Gets or sets the encrypted refresh token.
        /// </summary>
#pragma warning disable SA1011 // Closing square bracket should be followed by a space
        public byte[]? RefreshTokenEncrypted { get; set; }
#pragma warning restore SA1011 // Closing square bracket should be followed by a space

        /// <summary>
        /// Gets or sets the token expiration time.
        /// </summary>
        public DateTimeOffset? ExpiresAt { get; set; }

        /// <summary>
        /// Gets or sets the account status.
        /// </summary>
        public OAuthAccountStatus Status { get; set; }

        /// <summary>
        /// Gets or sets the creation timestamp.
        /// </summary>
        public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;

        /// <summary>
        /// Gets or sets the tenant navigation property.
        /// </summary>
        public Tenant? Tenant { get; set; }
    }
}
