// <copyright file="RefreshToken.cs" company="Synaxis">
// Copyright (c) Synaxis. All rights reserved.
// </copyright>

namespace Synaxis.Core.Models
{
    using System;
    using System.ComponentModel.DataAnnotations;

    /// <summary>
    /// Refresh token for extending user authentication sessions.
    /// </summary>
    public class RefreshToken
    {
        /// <summary>
        /// Gets or sets the unique identifier.
        /// </summary>
        public Guid Id { get; set; }

        /// <summary>
        /// Gets or sets the user identifier.
        /// </summary>
        public Guid UserId { get; set; }

        /// <summary>
        /// Gets or sets the user navigation property.
        /// </summary>
        public virtual User? User { get; set; }

        /// <summary>
        /// Gets or sets the hashed token value.
        /// </summary>
        [Required]
        public string TokenHash { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the creation timestamp.
        /// </summary>
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        /// <summary>
        /// Gets or sets the expiration timestamp.
        /// </summary>
        public DateTime ExpiresAt { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether the token has been revoked.
        /// </summary>
        public bool IsRevoked { get; set; }

        /// <summary>
        /// Gets or sets the revocation timestamp.
        /// </summary>
        public DateTime? RevokedAt { get; set; }

        /// <summary>
        /// Gets or sets the token hash that replaced this one (for rotation tracking).
        /// </summary>
        public string ReplacedByTokenHash { get; set; } = string.Empty;
    }
}
