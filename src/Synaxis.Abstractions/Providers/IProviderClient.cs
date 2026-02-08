// <copyright file="IProviderClient.cs" company="Synaxis">
// Copyright (c) Synaxis. All rights reserved.
// </copyright>

namespace Synaxis.Abstractions.Providers
{
    /// <summary>
    /// Marker interface for all provider client implementations.
    /// </summary>
    public interface IProviderClient
    {
        /// <summary>
        /// Gets the unique name of the provider.
        /// </summary>
        string ProviderName { get; }
    }
}
