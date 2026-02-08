// <copyright file="ChatGrpcService.cs" company="Synaxis">
// Copyright (c) Synaxis. All rights reserved.
// </copyright>

namespace Synaxis.Transport.Grpc.Services
{
    using System;
    using System.Linq;
    using System.Threading.Tasks;
    using global::Grpc.Core;
    using Mediator;
    using Microsoft.Extensions.Logging;
    using Synaxis.Commands.Chat;
    using Synaxis.Contracts.V1.Messages;
    using Synaxis.Transport.Grpc.V1;

    /// <summary>
    /// gRPC service implementation for chat completions.
    /// </summary>
    public sealed class ChatGrpcService : ChatService.ChatServiceBase
    {
        private readonly IMediator mediator;
        private readonly ILogger<ChatGrpcService> logger;

        /// <summary>
        /// Initializes a new instance of the <see cref="ChatGrpcService"/> class.
        /// </summary>
        /// <param name="mediator">The mediator for executing commands.</param>
        /// <param name="logger">The logger instance.</param>
        public ChatGrpcService(IMediator mediator, ILogger<ChatGrpcService> logger)
        {
            this.mediator = mediator ?? throw new ArgumentNullException(nameof(mediator));
            this.logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <summary>
        /// Creates a non-streaming chat completion.
        /// </summary>
        /// <param name="request">The chat request.</param>
        /// <param name="context">The server call context.</param>
        /// <returns>A <see cref="Task{ChatResponse}"/> representing the asynchronous operation.</returns>
        public override async Task<V1.ChatResponse> CreateCompletion(
            V1.ChatRequest request,
            ServerCallContext context)
        {
            this.logger.LogDebug("CreateCompletion called for model {Model}", request.Model);

            var command = new ChatCommand(
                Messages: request.Messages.Select(MapMessage).ToArray(),
                Model: request.Model,
                Temperature: request.HasTemperature ? request.Temperature : null,
                MaxTokens: request.HasMaxTokens ? request.MaxTokens : null,
                Provider: request.HasProvider ? request.Provider : null);

            var response = await this.mediator.Send(command, context.CancellationToken).ConfigureAwait(false);

            return MapResponse(response);
        }

        /// <summary>
        /// Creates a streaming chat completion.
        /// </summary>
        /// <param name="request">The chat request.</param>
        /// <param name="responseStream">The response stream to write chunks to.</param>
        /// <param name="context">The server call context.</param>
        /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
        public override async Task StreamCompletion(
            V1.ChatRequest request,
            IServerStreamWriter<V1.ChatStreamChunk> responseStream,
            ServerCallContext context)
        {
            this.logger.LogDebug("StreamCompletion called for model {Model}", request.Model);

            var command = new ChatStreamCommand(
                Messages: request.Messages.Select(MapMessage).ToArray(),
                Model: request.Model,
                Temperature: request.HasTemperature ? request.Temperature : null,
                MaxTokens: request.HasMaxTokens ? request.MaxTokens : null,
                Provider: request.HasProvider ? request.Provider : null);

            await foreach (var chunk in this.mediator.CreateStream(command, context.CancellationToken).ConfigureAwait(false))
            {
                await responseStream.WriteAsync(MapStreamChunk(chunk)).ConfigureAwait(false);
            }
        }

        private static ChatMessage MapMessage(V1.Message protoMessage)
        {
            return new ChatMessage
            {
                Role = protoMessage.Role,
                Content = protoMessage.Content,
                Name = protoMessage.HasName ? protoMessage.Name : null,
            };
        }

        private static V1.ChatResponse MapResponse(Contracts.V1.Messages.ChatResponse response)
        {
            var protoResponse = new V1.ChatResponse
            {
                Id = response.Id,
                Object = response.Object,
                Created = response.Created,
                Model = response.Model,
            };

            foreach (var choice in response.Choices)
            {
                protoResponse.Choices.Add(new V1.Choice
                {
                    Index = choice.Index,
                    Message = new V1.Message
                    {
                        Role = choice.Message.Role,
                        Content = choice.Message.Content,
                        Name = choice.Message.Name,
                    },
                    FinishReason = choice.FinishReason ?? string.Empty,
                });
            }

            if (response.Usage != null)
            {
                protoResponse.Usage = new V1.Usage
                {
                    PromptTokens = response.Usage.PromptTokens,
                    CompletionTokens = response.Usage.CompletionTokens,
                    TotalTokens = response.Usage.TotalTokens,
                };
            }

            return protoResponse;
        }

        private static V1.ChatStreamChunk MapStreamChunk(Contracts.V1.Messages.ChatStreamChunk chunk)
        {
            var protoChunk = new V1.ChatStreamChunk
            {
                Id = chunk.Id,
                Object = chunk.Object,
                Created = chunk.Created,
                Model = chunk.Model,
            };

            foreach (var choice in chunk.Choices)
            {
                var delta = new V1.MessageDelta();

                if (!string.IsNullOrEmpty(choice.Message?.Role))
                {
                    delta.Role = choice.Message.Role;
                }

                if (!string.IsNullOrEmpty(choice.Message?.Content))
                {
                    delta.Content = choice.Message.Content;
                }

                protoChunk.Choices.Add(new V1.ChoiceDelta
                {
                    Index = choice.Index,
                    Delta = delta,
                    FinishReason = choice.FinishReason,
                });
            }

            return protoChunk;
        }
    }
}
