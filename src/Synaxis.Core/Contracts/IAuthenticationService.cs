// <copyright file="IAuthenticationService.cs" company="Synaxis">
// Copyright (c) Synaxis. All rights reserved.
// </copyright>

namespace Synaxis.Core.Contracts
{
    using System;
    using System.Threading.Tasks;
    using Synaxis.Core.Models;

    /// <summary>
    /// Service for authentication operations including JWT token generation and validation.
    /// </summary>
    public interface IAuthenticationService
    {
        /// <summary>
        /// Authenticate a user with email and password.
        /// </summary>
        /// <param name="email">The user's email address.</param>
        /// <param name="password">The user's password.</param>
        /// <returns>A task that represents the asynchronous operation. The task result contains the authentication result.</returns>
        Task<AuthenticationResult> AuthenticateAsync(string email, string password);

        /// <summary>
        /// Refresh an access token using a refresh token.
        /// </summary>
        /// <param name="refreshToken">The refresh token.</param>
        /// <returns>A task that represents the asynchronous operation. The task result contains the authentication result.</returns>
        Task<AuthenticationResult> RefreshTokenAsync(string refreshToken);

        /// <summary>
        /// Logout a user by revoking their refresh token.
        /// </summary>
        /// <param name="refreshToken">The refresh token to revoke.</param>
        /// <returns>A task that represents the asynchronous operation.</returns>
        Task LogoutAsync(string refreshToken);

        /// <summary>
        /// Validate a JWT access token.
        /// </summary>
        /// <param name="token">The JWT token to validate.</param>
        /// <returns>True if the token is valid; otherwise, false.</returns>
        bool ValidateToken(string token);

        /// <summary>
        /// Get the user ID from a JWT token.
        /// </summary>
        /// <param name="token">The JWT token.</param>
        /// <returns>The user ID if valid; otherwise, null.</returns>
        Guid? GetUserIdFromToken(string token);
    }

    /// <summary>
    /// Represents the result of an authentication operation.
    /// </summary>
    public class AuthenticationResult
    {
        /// <summary>
        /// Gets or sets a value indicating whether the authentication was successful.
        /// </summary>
        public bool Success { get; set; }

        /// <summary>
        /// Gets or sets the JWT access token.
        /// </summary>
        public string AccessToken { get; set; }

        /// <summary>
        /// Gets or sets the refresh token.
        /// </summary>
        public string RefreshToken { get; set; }

        /// <summary>
        /// Gets or sets the access token expiration time in seconds.
        /// </summary>
        public int ExpiresIn { get; set; }

        /// <summary>
        /// Gets or sets the authenticated user.
        /// </summary>
        public User User { get; set; }

        /// <summary>
        /// Gets or sets the error message if authentication failed.
        /// </summary>
        public string ErrorMessage { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether MFA is required.
        /// </summary>
        public bool RequiresMfa { get; set; }
    }
}
