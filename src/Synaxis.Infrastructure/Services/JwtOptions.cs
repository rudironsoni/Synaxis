// <copyright file="JwtOptions.cs" company="Synaxis">
// Copyright (c) Synaxis. All rights reserved.
// </copyright>

namespace Synaxis.Infrastructure.Services
{
    /// <summary>
    /// Configuration options for JWT tokens.
    /// </summary>
    public class JwtOptions
    {
        /// <summary>
        /// Gets or sets the secret key for signing tokens.
        /// </summary>
        public string SecretKey { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the issuer.
        /// </summary>
        public string Issuer { get; set; } = "Synaxis";

        /// <summary>
        /// Gets or sets the audience.
        /// </summary>
        public string Audience { get; set; } = "SynaxisAPI";

        /// <summary>
        /// Gets or sets the access token expiration time in minutes.
        /// </summary>
        public int AccessTokenExpirationMinutes { get; set; } = 60;

        /// <summary>
        /// Gets or sets the refresh token expiration time in days.
        /// </summary>
        public int RefreshTokenExpirationDays { get; set; } = 7;
    }
}
