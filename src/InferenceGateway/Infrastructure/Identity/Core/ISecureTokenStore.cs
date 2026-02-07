// <copyright file="ISecureTokenStore.cs" company="PlaceholderCompany">
// Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>

namespace Synaxis.InferenceGateway.Infrastructure.Identity.Core
{
    using System.Collections.Generic;
    using System.Threading.Tasks;

    public interface ISecureTokenStore
    {
        Task SaveAsync(List<IdentityAccount> accounts);

        Task<List<IdentityAccount>> LoadAsync();
    }
}
