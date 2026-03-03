// <copyright file="ISecureTokenStore.cs" company="PlaceholderCompany">
// Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>

namespace Synaxis.InferenceGateway.Infrastructure.Identity.Core
{
    using System.Collections.Generic;
    using System.Threading.Tasks;

    /// <summary>
    /// Secure token store interface.
    /// </summary>
    public interface ISecureTokenStore
    {
        /// <summary>
        /// Saves the accounts.
        /// </summary>
        /// <param name="accounts">The accounts to save.</param>
        /// <returns>A task representing the asynchronous operation.</returns>
        Task SaveAsync(IList<IdentityAccount> accounts);

        /// <summary>
        /// Loads the accounts.
        /// </summary>
        /// <returns>The loaded accounts.</returns>
        Task<IList<IdentityAccount>> LoadAsync();
    }
}
