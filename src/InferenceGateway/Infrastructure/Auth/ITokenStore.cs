using System.Collections.Generic;
using System.Threading.Tasks;

namespace Synaxis.InferenceGateway.Infrastructure.Auth;

public interface ITokenStore
{
    Task<List<AntigravityAccount>> LoadAsync();
    Task SaveAsync(List<AntigravityAccount> accounts);
}
