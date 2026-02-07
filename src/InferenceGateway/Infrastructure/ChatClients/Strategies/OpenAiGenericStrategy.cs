// <copyright file="OpenAiGenericStrategy.cs" company="PlaceholderCompany">
// Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>

namespace Synaxis.InferenceGateway.Infrastructure.ChatClients.Strategies
{
    using System.Collections.Generic;
    using System.Threading;
    using System.Threading.Tasks;
    using Microsoft.Extensions.AI;
    using Synaxis.InferenceGateway.Application.ChatClients.Strategies;

    /// <summary>
    /// OpenAiGenericStrategy class.
    /// </summary>
    public class OpenAiGenericStrategy : IChatClientStrategy
    {
        private static readonly HashSet<string> SupportedTypes = new ()
        {
            "OpenAI", "Groq", "OpenRouter", "Pollinations", "Gemini", "Nvidia", "HuggingFace", "Cohere",
        };

        /// <inheritdoc/>
        public bool CanHandle(string providerType) => SupportedTypes.Contains(providerType);

        /// <inheritdoc/>
        public Task<ChatResponse> ExecuteAsync(
            IChatClient client,
            IEnumerable<ChatMessage> messages,
            ChatOptions options,
            CancellationToken ct)
        {
            return client.GetResponseAsync(messages, options, ct);
        }

        /// <inheritdoc/>
        public IAsyncEnumerable<ChatResponseUpdate> ExecuteStreamingAsync(
            IChatClient client,
            IEnumerable<ChatMessage> messages,
            ChatOptions options,
            CancellationToken ct)
        {
            return client.GetStreamingResponseAsync(messages, options, ct);
        }
    }
}
