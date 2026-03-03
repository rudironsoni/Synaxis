// <copyright file="ITokenStore.cs" company="PlaceholderCompany">
// Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>

namespace Synaxis.InferenceGateway.Infrastructure.Auth
{
    using System.Collections.Generic;
    using System.Threading.Tasks;

    /// <summary>
    /// Interface for storing and retrieving Antigravity account tokens.
    /// </summary>
    public interface ITokenStore
    {
        /// <summary>
        /// Loads Antigravity accounts from storage.
        /// </summary>
        /// <returns>A list of Antigravity accounts.</returns>
        Task<IList<AntigravityAccount>> LoadAsync();

        /// <summary>
        /// Saves Antigravity accounts to storage.
        /// </summary>
        /// <param name="accounts">The accounts to save.</param>
        /// <returns>A task representing the asynchronous operation.</returns>
        Task SaveAsync(IList<AntigravityAccount> accounts);
    }
}
