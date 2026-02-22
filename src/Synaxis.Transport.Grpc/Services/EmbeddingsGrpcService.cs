// <copyright file="EmbeddingsGrpcService.cs" company="Synaxis">
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
    using Synaxis.Commands.Embeddings;
    using Synaxis.Contracts.V1.Messages;
    using Synaxis.Transport.Grpc.V1;

    /// <summary>
    /// gRPC service implementation for embeddings generation.
    /// </summary>
    public sealed class EmbeddingsGrpcService : EmbeddingsService.EmbeddingsServiceBase
    {
        private readonly IMediator mediator;
        private readonly ILogger<EmbeddingsGrpcService> logger;

        /// <summary>
        /// Initializes a new instance of the <see cref="EmbeddingsGrpcService"/> class.
        /// </summary>
        /// <param name="mediator">The mediator for executing commands.</param>
        /// <param name="logger">The logger instance.</param>
        public EmbeddingsGrpcService(IMediator mediator, ILogger<EmbeddingsGrpcService> logger)
        {
            this.mediator = mediator!;
            this.logger = logger!;
        }

        /// <summary>
        /// Creates embeddings for the provided input.
        /// </summary>
        /// <param name="request">The embeddings request.</param>
        /// <param name="context">The server call context.</param>
        /// <returns>A <see cref="Task{EmbeddingsResponse}"/> representing the asynchronous operation.</returns>
        public override async Task<V1.EmbeddingsResponse> CreateEmbeddings(
            V1.EmbeddingsRequest request,
            ServerCallContext context)
        {
            this.logger.LogDebug("CreateEmbeddings called for model {Model}", request.Model);

            var command = new EmbeddingCommand(
                Input: request.Input.ToArray(),
                Model: request.Model,
                Provider: request.HasProvider ? request.Provider : null);

            var response = await this.mediator.Send(command, context.CancellationToken).ConfigureAwait(false);

            return MapResponse(response);
        }

        private static V1.EmbeddingsResponse MapResponse(Contracts.V1.Messages.EmbeddingResponse response)
        {
            var protoResponse = new V1.EmbeddingsResponse
            {
                Object = response.Object,
                Model = string.Empty, // Model not available in EmbeddingResponse
            };

            foreach (var data in response.Data)
            {
                var embedding = new V1.Embedding
                {
                    Object = data.Object,
                    Index = data.Index,
                };
                embedding.Embedding_.AddRange(data.Embedding);
                protoResponse.Data.Add(embedding);
            }

            if (response.Usage != null)
            {
                protoResponse.Usage = new V1.Usage
                {
                    PromptTokens = response.Usage.PromptTokens,
                    CompletionTokens = 0,
                    TotalTokens = response.Usage.TotalTokens,
                };
            }

            return protoResponse;
        }
    }
}
