// <copyright file="ICopilotSdkAdapter.cs" company="PlaceholderCompany">
// Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>

namespace Synaxis.InferenceGateway.Infrastructure.External.GitHub
{
    using System;
    using System.Collections.Generic;
    using System.Threading;
    using System.Threading.Tasks;
    using Microsoft.Extensions.AI;

    /// <summary>
    /// Adapter interface for GitHub Copilot SDK operations.
    /// Allows easy testing and separation of concrete GitHub.Copilot.SDK usage.
    /// </summary>
    public interface ICopilotSdkAdapter : IAsyncDisposable
    {
        /// <summary>
        /// Gets the metadata for the chat client.
        /// </summary>
        ChatClientMetadata Metadata { get; }

        /// <summary>
        /// Gets a response from the Copilot SDK asynchronously.
        /// </summary>
        /// <param name="messages">The chat messages.</param>
        /// <param name="options">Optional chat options.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>A task that resolves to a chat response.</returns>
        Task<ChatResponse> GetResponseAsync(IEnumerable<ChatMessage> messages, ChatOptions? options = null, CancellationToken cancellationToken = default);

        /// <summary>
        /// Gets a streaming response from the Copilot SDK.
        /// </summary>
        /// <param name="messages">The chat messages.</param>
        /// <param name="options">Optional chat options.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>An async enumerable of chat response updates.</returns>
        IAsyncEnumerable<ChatResponseUpdate> GetStreamingResponseAsync(IEnumerable<ChatMessage> messages, ChatOptions? options = null, CancellationToken cancellationToken = default);

        /// <summary>
        /// Gets a service of the specified type.
        /// </summary>
        /// <param name="serviceType">The type of service to retrieve.</param>
        /// <param name="serviceKey">Optional service key.</param>
        /// <returns>The service instance or null.</returns>
        object? GetService(Type serviceType, object? serviceKey = null);
    }
}