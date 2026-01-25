using System.Collections.Generic;
using System.Threading.Tasks;

namespace Synaxis.InferenceGateway.Infrastructure.Auth;

public record AccountInfo(string Email, bool IsActive);

public interface IAntigravityAuthManager : ITokenProvider
{
    IEnumerable<AccountInfo> ListAccounts();
    string StartAuthFlow(string redirectUrl);
    Task CompleteAuthFlowAsync(string code, string redirectUrl);
}
