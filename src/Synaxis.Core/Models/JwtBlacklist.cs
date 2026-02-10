// <copyright file="JwtBlacklist.cs" company="Synaxis">
// Copyright (c) Synaxis. All rights reserved.
// </copyright>

namespace Synaxis.Core.Models
{
    using System;
    using System.ComponentModel.DataAnnotations;

    /// <summary>
    /// Blacklisted JWT token for preventing reuse after logout.
    /// </summary>
    public class JwtBlacklist
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
        public virtual User User { get; set; }

        /// <summary>
        /// Gets or sets the JWT token identifier (jti claim).
        /// </summary>
        [Required]
        public string TokenId { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the creation timestamp.
        /// </summary>
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        /// <summary>
        /// Gets or sets the expiration timestamp of the original token.
        /// </summary>
        public DateTime ExpiresAt { get; set; }
    }
}
