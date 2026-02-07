// <copyright file="ITokenProvider.cs" company="PlaceholderCompany">
// Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>

namespace Synaxis.InferenceGateway.Infrastructure.Auth
{
    using System.Threading;
    using System.Threading.Tasks;

    /// <summary>
    /// Interface for providing authentication tokens for chat clients.
    /// </summary>
    public interface ITokenProvider
    {
        /// <summary>
        /// Gets a valid access token.
        /// </summary>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>A valid access token string.</returns>
        Task<string> GetTokenAsync(CancellationToken cancellationToken = default);
    }
}
