// <copyright file="IAntigravityAuthManager.cs" company="PlaceholderCompany">
// Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>

namespace Synaxis.InferenceGateway.Infrastructure.Auth
{
    using System.Collections.Generic;
    using System.Threading.Tasks;

    /// <summary>
    /// Represents account information for listing purposes.
    /// </summary>
    /// <param name="Email">The account email.</param>
    /// <param name="IsActive">Whether the account is active.</param>
    public record AccountInfo(string Email, bool IsActive);

    /// <summary>
    /// Interface for managing Antigravity authentication and tokens.
    /// </summary>
    public interface IAntigravityAuthManager : ITokenProvider
    {
        /// <summary>
        /// Lists all available accounts.
        /// </summary>
        /// <returns>An enumerable of account information.</returns>
        IEnumerable<AccountInfo> ListAccounts();

        /// <summary>
        /// Starts the OAuth authentication flow.
        /// </summary>
        /// <param name="redirectUrl">The redirect URL for OAuth callback.</param>
        /// <returns>The authorization URL to navigate to.</returns>
        string StartAuthFlow(string redirectUrl);

        /// <summary>
        /// Completes the OAuth authentication flow with an authorization code.
        /// </summary>
        /// <param name="code">The authorization code.</param>
        /// <param name="redirectUrl">The redirect URL used in the flow.</param>
        /// <param name="state">Optional state parameter.</param>
        /// <returns>A task representing the asynchronous operation.</returns>
        Task CompleteAuthFlowAsync(string code, string redirectUrl, string? state = null);
    }
}
