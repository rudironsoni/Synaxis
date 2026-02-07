// <copyright file="AntigravityAuthAdapter.cs" company="PlaceholderCompany">
// Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>

namespace Synaxis.InferenceGateway.Infrastructure.Identity.Strategies.Google
{
    using System;
    using System.Threading;
    using System.Threading.Tasks;
    using Synaxis.InferenceGateway.Infrastructure.Auth;
    using Synaxis.InferenceGateway.Infrastructure.Identity.Core;

    /// <summary>
    /// Adapter that bridges IdentityManager to IAntigravityAuthManager interface.
    /// </summary>
    public class AntigravityAuthAdapter : IAntigravityAuthManager
    {
        private readonly IdentityManager _identityManager;

        /// <summary>
        /// Initializes a new instance of the <see cref="AntigravityAuthAdapter"/> class.
        /// </summary>
        /// <param name="identityManager">The identity manager instance.</param>
        public AntigravityAuthAdapter(IdentityManager identityManager)
        {
            this._identityManager = identityManager ?? throw new ArgumentNullException(nameof(identityManager));
        }

        /// <inheritdoc/>
        public System.Collections.Generic.IEnumerable<AccountInfo> ListAccounts()
        {
            // Not supported via identity manager - return empty
            return Array.Empty<AccountInfo>();
        }

        /// <inheritdoc/>
        public string StartAuthFlow(string redirectUrl)
        {
            // IdentityManager.StartAuth is async; call synchronously by waiting on the Task
            var auth = this._identityManager.StartAuth("google").GetAwaiter().GetResult();
            return auth?.VerificationUri ?? string.Empty;
        }

        /// <inheritdoc/>
        public Task CompleteAuthFlowAsync(string code, string redirectUrl, string? state = null)
        {
            // Delegate to IdentityManager's new CompleteAuth (implemented below)
            return this._identityManager.CompleteAuth("google", code, state ?? string.Empty);
        }

        /// <inheritdoc/>
        public async Task<string> GetTokenAsync(CancellationToken cancellationToken = default)
        {
            var t = await this._identityManager.GetToken("google", cancellationToken).ConfigureAwait(false);
            return t ?? string.Empty;
        }
    }
}
