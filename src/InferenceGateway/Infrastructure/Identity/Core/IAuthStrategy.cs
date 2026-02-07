// <copyright file="IAuthStrategy.cs" company="PlaceholderCompany">
// Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>

namespace Synaxis.InferenceGateway.Infrastructure.Identity.Core
{
    using System;
    using System.Threading;
    using System.Threading.Tasks;

    /// <summary>
    /// Token response.
    /// </summary>
    public class TokenResponse
    {
        /// <summary>
        /// Gets or sets the access token.
        /// </summary>
        public string AccessToken { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the refresh token.
        /// </summary>
        public string? RefreshToken { get; set; }

        /// <summary>
        /// Gets or sets the expiration time in seconds.
        /// </summary>
        public int? ExpiresInSeconds { get; set; }
    }

    /// <summary>
    /// Authentication result.
    /// </summary>
    public class AuthResult
    {
        /// <summary>
        /// Gets or sets the status (e.g., Pending, Completed, Error).
        /// </summary>
        public string Status { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the user code.
        /// </summary>
        public string? UserCode { get; set; }

        /// <summary>
        /// Gets or sets the verification URI.
        /// </summary>
        public string? VerificationUri { get; set; }

        /// <summary>
        /// Gets or sets the message.
        /// </summary>
        public string? Message { get; set; }

        /// <summary>
        /// Gets or sets the token response.
        /// </summary>
        public TokenResponse? TokenResponse { get; set; }
    }

    /// <summary>
    /// Event arguments for account authentication.
    /// </summary>
    public class AccountAuthenticatedEventArgs : EventArgs
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="AccountAuthenticatedEventArgs"/> class.
        /// </summary>
        /// <param name="account">The authenticated account.</param>
        public AccountAuthenticatedEventArgs(IdentityAccount account)
        {
            this.Account = account;
        }

        /// <summary>
        /// Gets the authenticated account.
        /// </summary>
        public IdentityAccount Account { get; }
    }

    /// <summary>
    /// Authentication strategy interface.
    /// </summary>
    public interface IAuthStrategy
    {
        /// <summary>
        /// Occurs when an account is authenticated.
        /// </summary>
        event EventHandler<AccountAuthenticatedEventArgs>? AccountAuthenticated;

        /// <summary>
        /// Initiates the authentication flow.
        /// </summary>
        /// <param name="ct">Cancellation token.</param>
        /// <returns>The authentication result.</returns>
        Task<AuthResult> InitiateFlowAsync(CancellationToken ct);

        /// <summary>
        /// Completes the authentication flow.
        /// </summary>
        /// <param name="code">The authorization code.</param>
        /// <param name="state">The state parameter.</param>
        /// <param name="ct">Cancellation token.</param>
        /// <returns>The authentication result.</returns>
        Task<AuthResult> CompleteFlowAsync(string code, string state, CancellationToken ct);

        /// <summary>
        /// Refreshes the access token.
        /// </summary>
        /// <param name="account">The account to refresh.</param>
        /// <param name="ct">Cancellation token.</param>
        /// <returns>The new token response.</returns>
        Task<TokenResponse> RefreshTokenAsync(IdentityAccount account, CancellationToken ct);
    }
}
