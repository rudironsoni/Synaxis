using System;
using System.Threading;
using System.Threading.Tasks;

namespace Synaxis.InferenceGateway.Infrastructure.Identity.Core
{
    public class TokenResponse
    {
        public string AccessToken { get; set; } = string.Empty;
        public string? RefreshToken { get; set; }
        public int? ExpiresInSeconds { get; set; }
    }

    public class AuthResult
    {
        public string Status { get; set; } = string.Empty; // e.g., "Pending", "Completed", "Error"
        public string? UserCode { get; set; }
        public string? VerificationUri { get; set; }
        public string? Message { get; set; }
        public TokenResponse? TokenResponse { get; set; }
    }

    public interface IAuthStrategy
    {
        Task<AuthResult> InitiateFlowAsync(CancellationToken ct);
        Task<AuthResult> CompleteFlowAsync(string code, string state, CancellationToken ct);
        Task<TokenResponse> RefreshTokenAsync(IdentityAccount account, CancellationToken ct);
    }
}
