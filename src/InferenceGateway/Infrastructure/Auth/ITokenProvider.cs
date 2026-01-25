using System.Threading;
using System.Threading.Tasks;

namespace Synaxis.InferenceGateway.Infrastructure.Auth;

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
