// <copyright file="GeminiExtensions.cs" company="PlaceholderCompany">
// Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>

namespace Synaxis.InferenceGateway.Infrastructure.Extensions
{
    using System;
    using Microsoft.Extensions.AI;
    using Microsoft.Extensions.DependencyInjection;

    /// <summary>
    /// GeminiExtensions class.
    /// </summary>
    public static class GeminiExtensions
    {
        /// <summary>
        /// Adds a Google Gemini chat client to the service collection.
        /// </summary>
        /// <param name="services">The service collection.</param>
        /// <param name="apiKey">The API key for authentication.</param>
        /// <param name="modelId">The model identifier.</param>
        /// <returns>The service collection for chaining.</returns>
        public static IServiceCollection AddGeminiClient(this IServiceCollection services, string apiKey, string modelId)
        {
            ArgumentNullException.ThrowIfNull(services);

            services.AddChatClient(_ =>
            {
                var client = new Google.GenAI.Client(vertexAI: false, apiKey: apiKey);
                return new GeminiChatClient(client.AsIChatClient(modelId), client);
            });

            return services;
        }

        private sealed class GeminiChatClient : IChatClient
        {
            private readonly IChatClient _innerClient;
            private readonly IDisposable _disposableClient;

            public GeminiChatClient(IChatClient innerClient, IDisposable disposableClient)
            {
                ArgumentNullException.ThrowIfNull(innerClient);
                ArgumentNullException.ThrowIfNull(disposableClient);
                this._innerClient = innerClient;
                this._disposableClient = disposableClient;
            }

            public Task<ChatResponse> GetResponseAsync(IEnumerable<ChatMessage> messages, ChatOptions? options = null, CancellationToken cancellationToken = default)
            {
                return this._innerClient.GetResponseAsync(messages, options, cancellationToken);
            }

            public IAsyncEnumerable<ChatResponseUpdate> GetStreamingResponseAsync(IEnumerable<ChatMessage> messages, ChatOptions? options = null, CancellationToken cancellationToken = default)
            {
                return this._innerClient.GetStreamingResponseAsync(messages, options, cancellationToken);
            }

            public object? GetService(Type serviceType, object? serviceKey = null)
            {
                return this._innerClient.GetService(serviceType, serviceKey);
            }

            public void Dispose()
            {
                this._innerClient.Dispose();
                this._disposableClient.Dispose();
            }
        }
    }
}
