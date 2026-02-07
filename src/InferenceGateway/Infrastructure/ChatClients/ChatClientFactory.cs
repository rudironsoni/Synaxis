// <copyright file="ChatClientFactory.cs" company="PlaceholderCompany">
// Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>

namespace Synaxis.InferenceGateway.Infrastructure.ChatClients
{
    using System;
    using Microsoft.Extensions.AI;
    using Microsoft.Extensions.DependencyInjection;
    using Synaxis.InferenceGateway.Application.ChatClients;

    /// <summary>
    /// ChatClientFactory class.
    /// </summary>
    public class ChatClientFactory : IChatClientFactory
    {
        private readonly IServiceProvider _serviceProvider;

        /// <summary>
        /// Initializes a new instance of the <see cref="ChatClientFactory"/> class.
        /// </summary>
        /// <param name="serviceProvider">The serviceProvider.</param>
        public ChatClientFactory(IServiceProvider serviceProvider)
        {
            this._serviceProvider = serviceProvider;
        }

        /// <summary>
        /// Gets a chat client instance.
        /// </summary>
        /// <param name="key">Optional service key to retrieve a keyed service.</param>
        /// <returns>The chat client instance, or null if not found.</returns>
        public IChatClient? GetClient(string? key = null)
        {
            if (string.IsNullOrEmpty(key))
            {
                return this._serviceProvider.GetService<IChatClient>();
            }

            return this._serviceProvider.GetKeyedService<IChatClient>(key);
        }

        /// <summary>
        /// Gets a service instance of the specified type.
        /// </summary>
        /// <param name="serviceType">The type of service to retrieve.</param>
        /// <param name="serviceKey">Optional service key to retrieve a keyed service.</param>
        /// <returns>The service instance, or null if not found.</returns>
        public object? GetService(Type serviceType, object? serviceKey = null)
        {
            if (serviceKey == null)
            {
                return this._serviceProvider.GetService(serviceType);
            }

            return this._serviceProvider.GetKeyedService(serviceType, serviceKey);
        }
    }
}
