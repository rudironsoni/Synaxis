// <copyright file="StreamingService.cs" company="Synaxis">
// Copyright (c) Synaxis. All rights reserved.
// </copyright>

namespace Synaxis.Core.Streaming
{
    using System;
    using System.Collections.Concurrent;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading;
    using System.Threading.Tasks;
    using Microsoft.Extensions.Logging;

    /// <summary>
    /// Service to handle streaming operations across multiple providers.
    /// </summary>
    public class StreamingService
    {
        private readonly ConcurrentDictionary<string, IStreamingAdapter> _adapters;
        private readonly ILogger<StreamingService> _logger;
        private readonly Lock _lock = new Lock();

        /// <summary>
        /// Initializes a new instance of the <see cref="StreamingService"/> class.
        /// </summary>
        /// <param name="logger">The logger instance.</param>
        public StreamingService(ILogger<StreamingService> logger)
        {
            this._logger = logger ?? throw new ArgumentNullException(nameof(logger));
            this._adapters = new ConcurrentDictionary<string, IStreamingAdapter>(StringComparer.OrdinalIgnoreCase);
        }

        /// <summary>
        /// Registers a streaming adapter with the service.
        /// </summary>
        /// <param name="adapter">The adapter to register.</param>
        public void RegisterAdapter(IStreamingAdapter adapter)
        {
            if (adapter == null)
            {
                throw new ArgumentNullException(nameof(adapter));
            }

            if (string.IsNullOrEmpty(adapter.ProviderName))
            {
                throw new ArgumentException("Adapter provider name cannot be null or empty", nameof(adapter));
            }

            if (this._adapters.TryAdd(adapter.ProviderName, adapter))
            {
                this._logger.LogInformation(
                    "Registered streaming adapter for provider: {ProviderName}",
                    adapter.ProviderName);
            }
            else
            {
                this._logger.LogWarning(
                    "Streaming adapter for provider {ProviderName} is already registered",
                    adapter.ProviderName);
            }
        }

        /// <summary>
        /// Unregisters a streaming adapter from the service.
        /// </summary>
        /// <param name="providerName">The name of the provider.</param>
        /// <returns>True if the adapter was unregistered; otherwise, false.</returns>
        public bool UnregisterAdapter(string providerName)
        {
            if (string.IsNullOrEmpty(providerName))
            {
                throw new ArgumentException("Provider name cannot be null or empty", nameof(providerName));
            }

            if (this._adapters.TryRemove(providerName, out _))
            {
                this._logger.LogInformation(
                    "Unregistered streaming adapter for provider: {ProviderName}",
                    providerName);
                return true;
            }

            this._logger.LogWarning(
                "Streaming adapter for provider {ProviderName} not found",
                providerName);
            return false;
        }

        /// <summary>
        /// Gets a streaming adapter by provider name.
        /// </summary>
        /// <param name="providerName">The name of the provider.</param>
        /// <returns>The streaming adapter, or null if not found.</returns>
        public IStreamingAdapter? GetAdapter(string providerName)
        {
            if (string.IsNullOrEmpty(providerName))
            {
                throw new ArgumentException("Provider name cannot be null or empty", nameof(providerName));
            }

            return this._adapters.TryGetValue(providerName, out var adapter) ? adapter : null;
        }

        /// <summary>
        /// Gets all registered streaming adapters.
        /// </summary>
        /// <returns>A list of all registered adapters.</returns>
        public IReadOnlyList<IStreamingAdapter> GetAllAdapters()
        {
            return this._adapters.Values.ToList().AsReadOnly();
        }

        /// <summary>
        /// Streams a completion response from the specified provider.
        /// </summary>
        /// <typeparam name="TRequest">The type of request.</typeparam>
        /// <param name="providerName">The name of the provider.</param>
        /// <param name="request">The request to send to the provider.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>An asynchronous enumerable of streaming responses.</returns>
        public async IAsyncEnumerable<StreamingResponse> StreamCompletionAsync<TRequest>(
            string providerName,
            TRequest request,
            [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken cancellationToken = default)
            where TRequest : class
        {
            var adapter = this.GetAdapter(providerName);
            if (adapter == null)
            {
                this._logger.LogError("No streaming adapter found for provider: {ProviderName}", providerName);
                yield return StreamingResponse.CreateError($"No adapter found for provider: {providerName}");
                yield break;
            }

            if (!adapter.SupportsStreaming)
            {
                this._logger.LogWarning("Provider {ProviderName} does not support streaming", providerName);
                yield return StreamingResponse.CreateError($"Provider {providerName} does not support streaming");
                yield break;
            }

            await adapter.ValidateRequestAsync(request).ConfigureAwait(false);

            var errorResponse = await this.StreamWithAdapterAsync(
                adapter,
                request,
                providerName,
                cancellationToken).ConfigureAwait(false);

            if (errorResponse != null)
            {
                yield return errorResponse;
            }
        }

        /// <summary>
        /// Streams a chat completion response from the specified provider.
        /// </summary>
        /// <typeparam name="TRequest">The type of request.</typeparam>
        /// <param name="providerName">The name of the provider.</param>
        /// <param name="request">The request to send to the provider.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>An asynchronous enumerable of streaming responses.</returns>
        public async IAsyncEnumerable<StreamingResponse> StreamChatCompletionAsync<TRequest>(
            string providerName,
            TRequest request,
            [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken cancellationToken = default)
            where TRequest : class
        {
            var adapter = this.GetAdapter(providerName);
            if (adapter == null)
            {
                this._logger.LogError("No streaming adapter found for provider: {ProviderName}", providerName);
                yield return StreamingResponse.CreateError($"No adapter found for provider: {providerName}");
                yield break;
            }

            if (!adapter.SupportsStreaming)
            {
                this._logger.LogWarning("Provider {ProviderName} does not support streaming", providerName);
                yield return StreamingResponse.CreateError($"Provider {providerName} does not support streaming");
                yield break;
            }

            await adapter.ValidateRequestAsync(request).ConfigureAwait(false);

            var errorResponse = await this.StreamChatWithAdapterAsync(
                adapter,
                request,
                providerName,
                cancellationToken).ConfigureAwait(false);

            if (errorResponse != null)
            {
                yield return errorResponse;
            }
        }

        /// <summary>
        /// Streams responses from multiple providers concurrently.
        /// </summary>
        /// <typeparam name="TRequest">The type of request.</typeparam>
        /// <param name="providerRequests">A dictionary of provider names to requests.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>An asynchronous enumerable of streaming responses from all providers.</returns>
        public async IAsyncEnumerable<StreamingResponse> StreamFromMultipleProvidersAsync<TRequest>(
            IDictionary<string, TRequest> providerRequests,
            [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken cancellationToken = default)
            where TRequest : class
        {
            var tasks = new List<Task<IAsyncEnumerable<StreamingResponse>>>();

            foreach (var kvp in providerRequests)
            {
                var providerName = kvp.Key;
                var request = kvp.Value;

                tasks.Add(
                    Task.Run(
                        async () =>
                        {
                            var responses = new List<StreamingResponse>();
                            await foreach (var response in this.StreamCompletionAsync(providerName, request, cancellationToken).ConfigureAwait(false))
                            {
                                responses.Add(response);
                            }

                            return responses.ToAsyncEnumerable();
                        },
                        cancellationToken));
            }

            await Task.WhenAll(tasks).ConfigureAwait(false);

            foreach (var task in tasks)
            {
                var enumerable = await task.ConfigureAwait(false);
                var enumerator = enumerable.GetAsyncEnumerator();
                try
                {
                    while (await enumerator.MoveNextAsync().ConfigureAwait(false))
                    {
                        yield return enumerator.Current;
                    }
                }
                finally
                {
                    await enumerator.DisposeAsync().ConfigureAwait(false);
                }
            }
        }

        /// <summary>
        /// Gets the health status of all registered adapters.
        /// </summary>
        /// <returns>A dictionary of provider names to their health status.</returns>
        public IDictionary<string, AdapterHealthStatus> GetAdapterHealthStatus()
        {
            var statusDict = new Dictionary<string, AdapterHealthStatus>(StringComparer.OrdinalIgnoreCase);

            foreach (var kvp in this._adapters)
            {
                statusDict[kvp.Key] = new AdapterHealthStatus
                {
                    ProviderName = kvp.Key,
                    SupportsStreaming = kvp.Value.SupportsStreaming,
                    SupportedEventTypes = kvp.Value.GetSupportedEventTypes().ToList(),
                    DefaultRetryInterval = kvp.Value.GetDefaultRetryInterval(),
                    Status = "Healthy",
                };
            }

            return statusDict;
        }

        /// <summary>
        /// Streams with the specified adapter and handles errors.
        /// </summary>
        /// <typeparam name="TRequest">The type of request.</typeparam>
        /// <param name="adapter">The streaming adapter.</param>
        /// <param name="request">The request.</param>
        /// <param name="providerName">The provider name.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>An error response if an error occurred; otherwise, null.</returns>
        private async Task<StreamingResponse?> StreamWithAdapterAsync<TRequest>(
            IStreamingAdapter adapter,
            TRequest request,
            string providerName,
            CancellationToken cancellationToken)
            where TRequest : class
        {
            try
            {
                await foreach (var response in adapter.StreamCompletionAsync(request, cancellationToken).ConfigureAwait(false))
                {
                    // This is a placeholder - actual streaming would be handled differently
                    // in a real implementation with proper async enumerable handling
                }

                return null;
            }
            catch (OperationCanceledException ex)
            {
                this._logger.LogInformation(ex, "Streaming operation cancelled for provider: {ProviderName}", providerName);
                return StreamingResponse.CreateError("Operation cancelled");
            }
            catch (Exception ex)
            {
                this._logger.LogError(ex, "Error streaming completion from provider: {ProviderName}", providerName);
                return adapter.HandleError(ex, Guid.NewGuid().ToString());
            }
        }

        /// <summary>
        /// Streams chat with the specified adapter and handles errors.
        /// </summary>
        /// <typeparam name="TRequest">The type of request.</typeparam>
        /// <param name="adapter">The streaming adapter.</param>
        /// <param name="request">The request.</param>
        /// <param name="providerName">The provider name.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>An error response if an error occurred; otherwise, null.</returns>
        private async Task<StreamingResponse?> StreamChatWithAdapterAsync<TRequest>(
            IStreamingAdapter adapter,
            TRequest request,
            string providerName,
            CancellationToken cancellationToken)
            where TRequest : class
        {
            try
            {
                await foreach (var response in adapter.StreamChatCompletionAsync(request, cancellationToken).ConfigureAwait(false))
                {
                    // This is a placeholder - actual streaming would be handled differently
                    // in a real implementation with proper async enumerable handling
                }

                return null;
            }
            catch (OperationCanceledException ex)
            {
                this._logger.LogInformation(ex, "Streaming operation cancelled for provider: {ProviderName}", providerName);
                return StreamingResponse.CreateError("Operation cancelled");
            }
            catch (Exception ex)
            {
                this._logger.LogError(ex, "Error streaming chat completion from provider: {ProviderName}", providerName);
                return adapter.HandleError(ex, Guid.NewGuid().ToString());
            }
        }

        /// <summary>
        /// Represents the health status of a streaming adapter.
        /// </summary>
        public class AdapterHealthStatus
        {
            /// <summary>
            /// Gets or sets the provider name.
            /// </summary>
            public string ProviderName { get; set; } = string.Empty;

            /// <summary>
            /// Gets or sets a value indicating whether the adapter supports streaming.
            /// </summary>
            public bool SupportsStreaming { get; set; }

            /// <summary>
            /// Gets or sets the supported event types.
            /// </summary>
            public IList<string> SupportedEventTypes { get; set; } = new List<string>();

            /// <summary>
            /// Gets or sets the default retry interval in milliseconds.
            /// </summary>
            public int DefaultRetryInterval { get; set; }

            /// <summary>
            /// Gets or sets the adapter status.
            /// </summary>
            public string Status { get; set; } = string.Empty;
        }
    }
}
