// <copyright file="IAuthStrategy.cs" company="PlaceholderCompany">
// Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>

namespace Synaxis.InferenceGateway.Infrastructure.Identity.Core
{
    using System;
    using System.Threading;
    using System.Threading.Tasks;

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

    public class AccountAuthenticatedEventArgs : EventArgs
    {
        public IdentityAccount Account { get; }

        public AccountAuthenticatedEventArgs(IdentityAccount account)
        {
            Account = account;
        }
    }

    public interface IAuthStrategy
    {
        event EventHandler<AccountAuthenticatedEventArgs>? AccountAuthenticated;

        Task<AuthResult> InitiateFlowAsync(CancellationToken ct);

        Task<AuthResult> CompleteFlowAsync(string code, string state, CancellationToken ct);

        Task<TokenResponse> RefreshTokenAsync(IdentityAccount account, CancellationToken ct);
    }
}
