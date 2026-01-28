using System;
using System.Threading;
using System.Threading.Tasks;
using Synaxis.InferenceGateway.Infrastructure.Auth;
using Synaxis.InferenceGateway.Infrastructure.Identity.Core;

namespace Synaxis.InferenceGateway.Infrastructure.Identity.Strategies.Google
{
    public class AntigravityAuthAdapter : IAntigravityAuthManager
    {
        private readonly IdentityManager _identityManager;

        public AntigravityAuthAdapter(IdentityManager identityManager)
        {
            _identityManager = identityManager ?? throw new ArgumentNullException(nameof(identityManager));
        }

        public System.Collections.Generic.IEnumerable<AccountInfo> ListAccounts()
        {
            // Not supported via identity manager - return empty
            return Array.Empty<AccountInfo>();
        }

        public string StartAuthFlow(string redirectUrl)
        {
            // IdentityManager.StartAuth is async; call synchronously by waiting on the Task
            var auth = _identityManager.StartAuth("google").GetAwaiter().GetResult();
            return auth?.VerificationUri ?? string.Empty;
        }

        public Task CompleteAuthFlowAsync(string code, string redirectUrl, string? state = null)
        {
            // Delegate to IdentityManager's new CompleteAuth (implemented below)
            return _identityManager.CompleteAuth("google", code, state ?? string.Empty);
        }

        public async Task<string> GetTokenAsync(CancellationToken cancellationToken = default)
        {
            var t = await _identityManager.GetToken("google", cancellationToken).ConfigureAwait(false);
            return t ?? string.Empty;
        }
    }
}
