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

    public class CloudflareStrategy : IChatClientStrategy
    {
        public bool CanHandle(string providerType) => providerType == "Cloudflare";

        public async Task<ChatResponse> ExecuteAsync(
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
            return await client.GetResponseAsync(messages, options, ct);
        }

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