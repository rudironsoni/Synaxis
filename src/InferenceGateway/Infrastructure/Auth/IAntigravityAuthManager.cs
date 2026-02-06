// <copyright file="IAntigravityAuthManager.cs" company="PlaceholderCompany">
// Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>

namespace Synaxis.InferenceGateway.Infrastructure.Auth
{
    using System.Collections.Generic;
    using System.Threading.Tasks;

    public record AccountInfo(string Email, bool IsActive);

    public interface IAntigravityAuthManager : ITokenProvider
    {
        IEnumerable<AccountInfo> ListAccounts();
        string StartAuthFlow(string redirectUrl);
        Task CompleteAuthFlowAsync(string code, string redirectUrl, string? state = null);
    }
}