// <copyright file="GeminiExtensions.cs" company="Synaxis">
// Copyright (c) Synaxis. All rights reserved.
// </copyright>

namespace Synaxis.InferenceGateway.Infrastructure.Extensions
{
    using System;
    using Microsoft.Extensions.AI;
    using Microsoft.Extensions.DependencyInjection;

    /// <summary>
    /// Extensions for registering Gemini chat clients.
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

            services.AddChatClient(sp =>
                ActivatorUtilities.CreateInstance<GeminiChatClient>(sp, apiKey, modelId));

            return services;
        }

        private sealed class GeminiChatClient : IChatClient
        {
            private readonly Google.GenAI.Client _genAiClient;
            private readonly IChatClient _innerClient;

            public GeminiChatClient(string apiKey, string modelId)
            {
                ArgumentException.ThrowIfNullOrWhiteSpace(apiKey);
                ArgumentException.ThrowIfNullOrWhiteSpace(modelId);

                this._genAiClient = new Google.GenAI.Client(vertexAI: false, apiKey: apiKey);
                this._innerClient = this._genAiClient.AsIChatClient(modelId);
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
                this._genAiClient.Dispose();
            }
        }
    }
}
