// <copyright file="CopilotSdkClient.cs" company="PlaceholderCompany">
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
    public interface ICopilotSdkAdapter : IDisposable
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

    /// <summary>
    /// IChatClient implementation backed by a GitHub Copilot SDK adapter.
    /// Keeps production wiring separate from tests and allows graceful lifecycle management.
    /// </summary>
    public sealed class CopilotSdkClient : IChatClient
    {
        private readonly ICopilotSdkAdapter _adapter;

        /// <summary>
        /// Initializes a new instance of the <see cref="CopilotSdkClient"/> class.
        /// </summary>
        /// <param name="adapter">The Copilot SDK adapter.</param>
        public CopilotSdkClient(ICopilotSdkAdapter adapter)
        {
            this._adapter = adapter ?? throw new ArgumentNullException(nameof(adapter));
        }

        // Note: For scenarios where the SDK is available at runtime a factory/wrapper can be
        // written to produce an ICopilotSdkAdapter that talks to GitHub.Copilot.SDK. That factory
        // is intentionally not included here to keep the client lightweight and testable.

        /// <summary>
        /// Gets the metadata for the chat client.
        /// </summary>
        public ChatClientMetadata Metadata => this._adapter.Metadata ?? new ChatClientMetadata("Copilot");

        /// <inheritdoc/>
        public Task<ChatResponse> GetResponseAsync(IEnumerable<ChatMessage> messages, ChatOptions? options = null, CancellationToken cancellationToken = default)
        {
            return this._adapter.GetResponseAsync(messages, options, cancellationToken);
        }

        /// <inheritdoc/>
        public IAsyncEnumerable<ChatResponseUpdate> GetStreamingResponseAsync(IEnumerable<ChatMessage> messages, ChatOptions? options = null, CancellationToken cancellationToken = default)
        {
            return this._adapter.GetStreamingResponseAsync(messages, options, cancellationToken);
        }

        /// <inheritdoc/>
        public object? GetService(Type serviceType, object? serviceKey = null)
        {
            return this._adapter.GetService(serviceType, serviceKey);
        }

        /// <inheritdoc/>
        public void Dispose()
        {
            // Note: _adapter is injected and should not be disposed by this class.
            // The caller/DI container is responsible for disposing the injected ICopilotSdkAdapter.
        }
    }
}
