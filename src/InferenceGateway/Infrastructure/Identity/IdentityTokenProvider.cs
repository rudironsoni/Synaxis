// <copyright file="IdentityTokenProvider.cs" company="PlaceholderCompany">
// Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>

namespace Synaxis.InferenceGateway.Infrastructure.Identity
{
    using System.Threading;
    using System.Threading.Tasks;
    using Synaxis.InferenceGateway.Infrastructure.Auth;
    using Synaxis.InferenceGateway.Infrastructure.Identity.Core;

    /// <summary>
    /// Identity-based token provider.
    /// </summary>
    public class IdentityTokenProvider : ITokenProvider
    {
        private readonly IdentityManager _manager;

        /// <summary>
        /// Initializes a new instance of the <see cref="IdentityTokenProvider"/> class.
        /// </summary>
        /// <param name="manager">The identity manager.</param>
        public IdentityTokenProvider(IdentityManager manager)
        {
            this._manager = manager;
        }

        /// <inheritdoc/>
        public async Task<string> GetTokenAsync(CancellationToken cancellationToken = default)
        {
            var t = await this._manager.GetToken("google", cancellationToken).ConfigureAwait(false);
            return t ?? string.Empty;
        }
    }
}
