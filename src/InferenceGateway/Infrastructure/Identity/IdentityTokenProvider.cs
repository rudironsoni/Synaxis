using System.Threading;
using System.Threading.Tasks;
using Synaxis.InferenceGateway.Infrastructure.Auth;
using Synaxis.InferenceGateway.Infrastructure.Identity.Core;

namespace Synaxis.InferenceGateway.Infrastructure.Identity
{
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
