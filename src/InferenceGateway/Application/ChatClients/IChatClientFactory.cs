// <copyright file="IChatClientFactory.cs" company="PlaceholderCompany">
// Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>

namespace Synaxis.InferenceGateway.Application.ChatClients
{
    using System;
    using Microsoft.Extensions.AI;

    /// <summary>
    /// Factory for creating chat clients.
    /// </summary>
    public interface IChatClientFactory
    {
        /// <summary>
        /// Resolve the IChatClient for the current request context or by provider key.
        /// If key is null, returns the primary IChatClient for the request.
        /// </summary>
        /// <param name="key">The provider key.</param>
        /// <returns>The chat client instance.</returns>
        IChatClient? GetClient(string? key = null);

        /// <summary>
        /// Generic service accessor used by chat clients that expose GetService mirrors.
        /// </summary>
        /// <param name="serviceType">The service type to resolve.</param>
        /// <param name="serviceKey">The service key.</param>
        /// <returns>The service instance.</returns>
        object? GetService(Type serviceType, object? serviceKey = null);
    }
}
