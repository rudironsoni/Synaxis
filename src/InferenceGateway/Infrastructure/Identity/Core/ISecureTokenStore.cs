using System.Collections.Generic;
using System.Threading.Tasks;

namespace Synaxis.InferenceGateway.Infrastructure.Identity.Core
{
    public interface ISecureTokenStore
    {
        Task SaveAsync(List<IdentityAccount> accounts);
        Task<List<IdentityAccount>> LoadAsync();
    }
}
