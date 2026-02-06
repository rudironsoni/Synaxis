// <copyright file="UsageTrackingChatClient.cs" company="PlaceholderCompany">
// Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>

namespace Synaxis.InferenceGateway.Application.ChatClients
{
    using System.Collections.Generic;
    using System.Runtime.CompilerServices;
    using System.Threading;
    using System.Threading.Tasks;
    using Microsoft.Extensions.AI;
    using Microsoft.Extensions.Logging;

    /// <summary>
    /// Chat client that tracks usage and costs.
    /// </summary>
    public class UsageTrackingChatClient : DelegatingChatClient
    {
        private readonly ILogger<UsageTrackingChatClient> logger;

        /// <summary>
        /// Initializes a new instance of the <see cref="UsageTrackingChatClient"/> class.
        /// </summary>
        /// <param name="innerClient">The inner chat client.</param>
        /// <param name="logger">The logger instance.</param>
        public UsageTrackingChatClient(IChatClient innerClient, ILogger<UsageTrackingChatClient> logger)
            : base(innerClient)
        {
            this.logger = logger;
        }

        /// <summary>
        /// Gets a chat response and tracks usage.
        /// </summary>
        /// <param name="messages">The chat messages.</param>
        /// <param name="options">The chat options.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>The chat response.</returns>
        public override async Task<ChatResponse> GetResponseAsync(
            IEnumerable<ChatMessage> messages,
            ChatOptions? options = null,
            CancellationToken cancellationToken = default)
        {
            var response = await base.GetResponseAsync(messages, options, cancellationToken).ConfigureAwait(false);

            if (response.Usage != null)
            {
                var model = response.ModelId ?? options?.ModelId ?? "unknown";
                var inputTokens = response.Usage.InputTokenCount ?? 0;
                var outputTokens = response.Usage.OutputTokenCount ?? 0;
                var cost = CalculateCost(model, inputTokens, outputTokens);

                this.logger.LogInformation(
                    "Model: {Model}, Input: {Input}, Output: {Output}, Cost: {Cost:C6}",
                    model,
                    inputTokens,
                    outputTokens,
                    cost);
            }

            return response;
        }

        /// <summary>
        /// Gets a streaming chat response.
        /// </summary>
        /// <param name="messages">The chat messages.</param>
        /// <param name="options">The chat options.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>The chat response updates.</returns>
        public override async IAsyncEnumerable<ChatResponseUpdate> GetStreamingResponseAsync(
            IEnumerable<ChatMessage> messages,
            ChatOptions? options = null,
            [EnumeratorCancellation] CancellationToken cancellationToken = default)
        {
            await foreach (var update in base.GetStreamingResponseAsync(messages, options, cancellationToken).ConfigureAwait(false))
            {
                yield return update;
            }
        }

        private static decimal CalculateCost(string model, long inputTokens, long outputTokens)
        {
            decimal inputRate = 0;
            decimal outputRate = 0;

            string lowerModel = model.ToLowerInvariant();

            if (lowerModel.Contains("llama") || lowerModel.Contains("mixtral") || lowerModel.Contains("gemma"))
            {
                inputRate = 0.70m / 1_000_000m;
                outputRate = 0.80m / 1_000_000m;
            }
            else if (lowerModel.Contains("gemini"))
            {
                inputRate = 0.075m / 1_000_000m;
                outputRate = 0.30m / 1_000_000m;
            }

            return (inputTokens * inputRate) + (outputTokens * outputRate);
        }
    }
}
