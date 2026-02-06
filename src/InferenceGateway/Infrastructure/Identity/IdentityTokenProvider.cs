// <copyright file="IdentityTokenProvider.cs" company="PlaceholderCompany">
// Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>

namespace Synaxis.InferenceGateway.Infrastructure.Identity
{
    using System.Threading;
    using System.Threading.Tasks;
    using Synaxis.InferenceGateway.Infrastructure.Auth;
    using Synaxis.InferenceGateway.Infrastructure.Identity.Core;

    public class IdentityTokenProvider : ITokenProvider
    {
        private readonly IdentityManager _manager;

        public IdentityTokenProvider(IdentityManager manager)
        {
            _manager = manager;
        }

        public async Task<string> GetTokenAsync(CancellationToken cancellationToken = default)
        {
            var t = await _manager.GetToken("google", cancellationToken).ConfigureAwait(false);
            return t ?? string.Empty;
        }
    }
}
