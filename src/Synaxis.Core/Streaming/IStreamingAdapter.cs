// <copyright file="IStreamingAdapter.cs" company="Synaxis">
// Copyright (c) Synaxis. All rights reserved.
// </copyright>

namespace Synaxis.Core.Streaming
{
    using System;
    using System.Collections.Generic;
    using System.Threading;
    using System.Threading.Tasks;

    /// <summary>
    /// Defines the contract for provider-specific streaming adapters.
    /// </summary>
    public interface IStreamingAdapter
    {
        /// <summary>
        /// Gets the provider name (e.g., "openai", "anthropic", "google-ai").
        /// </summary>
        string ProviderName { get; }

        /// <summary>
        /// Gets a value indicating whether the adapter supports streaming.
        /// </summary>
        bool SupportsStreaming { get; }

        /// <summary>
        /// Streams a completion response from the provider.
        /// </summary>
        /// <typeparam name="TRequest">The type of request.</typeparam>
        /// <param name="request">The request to send to the provider.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>An asynchronous enumerable of streaming responses.</returns>
        IAsyncEnumerable<StreamingResponse> StreamCompletionAsync<TRequest>(
            TRequest request,
            CancellationToken cancellationToken = default)
            where TRequest : class;

        /// <summary>
        /// Streams a chat completion response from the provider.
        /// </summary>
        /// <typeparam name="TRequest">The type of request.</typeparam>
        /// <param name="request">The request to send to the provider.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>An asynchronous enumerable of streaming responses.</returns>
        IAsyncEnumerable<StreamingResponse> StreamChatCompletionAsync<TRequest>(
            TRequest request,
            CancellationToken cancellationToken = default)
            where TRequest : class;

        /// <summary>
        /// Validates the streaming request before sending it to the provider.
        /// </summary>
        /// <typeparam name="TRequest">The type of request.</typeparam>
        /// <param name="request">The request to validate.</param>
        /// <returns>A task representing the asynchronous validation operation.</returns>
        Task ValidateRequestAsync<TRequest>(TRequest request)
            where TRequest : class;

        /// <summary>
        /// Converts a provider-specific response to a streaming response.
        /// </summary>
        /// <typeparam name="TProviderResponse">The type of provider response.</typeparam>
        /// <param name="providerResponse">The provider response to convert.</param>
        /// <param name="metadata">The metadata to include in the response.</param>
        /// <returns>The converted streaming response.</returns>
        StreamingResponse ConvertToStreamingResponse<TProviderResponse>(
            TProviderResponse providerResponse,
            StreamingMetadata? metadata = null)
            where TProviderResponse : class;

        /// <summary>
        /// Handles errors that occur during streaming.
        /// </summary>
        /// <param name="exception">The exception that occurred.</param>
        /// <param name="requestId">The request ID.</param>
        /// <returns>An error streaming response.</returns>
        StreamingResponse HandleError(Exception exception, string requestId);

        /// <summary>
        /// Gets the default retry interval for the provider.
        /// </summary>
        /// <returns>The retry interval in milliseconds.</returns>
        int GetDefaultRetryInterval();

        /// <summary>
        /// Gets the supported event types for the provider.
        /// </summary>
        /// <returns>A list of supported event types.</returns>
        IEnumerable<string> GetSupportedEventTypes();
    }
}
