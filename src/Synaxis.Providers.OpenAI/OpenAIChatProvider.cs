// <copyright file="OpenAIChatProvider.cs" company="Synaxis">
// Copyright (c) Synaxis. All rights reserved.
// </copyright>

namespace Synaxis.Providers.OpenAI
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading;
    using System.Threading.Tasks;
    using Microsoft.Extensions.Logging;
    using Synaxis.Abstractions.Providers;
    using Synaxis.Contracts.V1.Messages;
    using Synaxis.Providers.OpenAI.Models;

    /// <summary>
    /// OpenAI implementation of <see cref="IChatProvider"/>.
    /// </summary>
    public sealed class OpenAIChatProvider : IChatProvider
    {
        private readonly OpenAIClient _client;

        /// <summary>
        /// Initializes a new instance of the <see cref="OpenAIChatProvider"/> class.
        /// </summary>
        /// <param name="client">The OpenAI client.</param>
        /// <param name="logger">The logger.</param>
        public OpenAIChatProvider(
            OpenAIClient client,
            ILogger<OpenAIChatProvider> logger)
        {
            this._client = client ?? throw new ArgumentNullException(nameof(client));
            _ = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <inheritdoc/>
        public string ProviderName => "OpenAI";

        /// <inheritdoc/>
        public async Task<object> ChatAsync(
            IEnumerable<object> messages,
            string model,
            object? options = null,
            CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(messages);
            ArgumentNullException.ThrowIfNull(model);

            var request = this.BuildChatRequest(messages, model);
            var response = await this._client.PostAsync<OpenAIChatRequest, OpenAIChatResponse>(
                "chat/completions",
                request,
                cancellationToken).ConfigureAwait(false);

            return this.MapToSynaxisResponse(response);
        }

        private OpenAIChatRequest BuildChatRequest(
            IEnumerable<object> messages,
            string model)
        {
            var openAIMessages = messages
                .Cast<ChatMessage>()
                .Select(m => new OpenAIMessage
                {
                    Role = m.Role,
                    Content = m.Content,
                    Name = m.Name,
                })
                .ToList();

            return new OpenAIChatRequest
            {
                Model = model,
                Messages = openAIMessages,
                Stream = false,
            };
        }

        private ChatResponse MapToSynaxisResponse(OpenAIChatResponse response)
        {
            var choices = response.Choices
                .Select(c => new ChatChoice
                {
                    Index = c.Index,
                    Message = new ChatMessage
                    {
                        Role = c.Message?.Role ?? "assistant",
                        Content = c.Message?.Content ?? string.Empty,
                        Name = c.Message?.Name,
                    },
                    FinishReason = c.FinishReason,
                })
                .ToArray();

            ChatUsage? usage = null;
            if (response.Usage is not null)
            {
                usage = new ChatUsage
                {
                    PromptTokens = response.Usage.PromptTokens,
                    CompletionTokens = response.Usage.CompletionTokens,
                    TotalTokens = response.Usage.TotalTokens,
                };
            }

            return new ChatResponse
            {
                Id = response.Id,
                Object = response.Object,
                Created = response.Created,
                Model = response.Model,
                Choices = choices,
                Usage = usage,
            };
        }
    }
}
