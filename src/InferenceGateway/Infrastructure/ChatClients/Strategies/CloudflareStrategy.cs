// <copyright file="CloudflareStrategy.cs" company="PlaceholderCompany">
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
    /// CloudflareStrategy class.
    /// </summary>
    public class CloudflareStrategy : IChatClientStrategy
    {
        /// <inheritdoc/>
        public bool CanHandle(string providerType) => string.Equals(providerType, "Cloudflare", System.StringComparison.Ordinal);

        /// <inheritdoc/>
        public Task<ChatResponse> ExecuteAsync(
            IChatClient client,
            IEnumerable<ChatMessage> messages,
            ChatOptions options,
            CancellationToken ct)
        {
            // Standard pass-through: Cloudflare-specific behavior (URL, auth,
            // request formatting, streaming framing, default headers) is implemented
            // in CloudflareChatClient. Keep this strategy lightweight so the client
            // handles protocol details. This strategy exists to allow future
            // provider-specific adjustments (e.g. token limits or option remapping)
            // without changing callers.
            return client.GetResponseAsync(messages, options, ct);
        }

        /// <inheritdoc/>
        public IAsyncEnumerable<ChatResponseUpdate> ExecuteStreamingAsync(
            IChatClient client,
            IEnumerable<ChatMessage> messages,
            ChatOptions options,
            CancellationToken ct)
        {
            // Streaming is handled by the CloudflareChatClient which knows the
            // server-side streaming framing. This is a direct pass-through.
            return client.GetStreamingResponseAsync(messages, options, ct);
        }
    }
}
