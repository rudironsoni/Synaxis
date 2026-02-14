// <copyright file="EmbeddingMicroservice.cs" company="Synaxis">
// Copyright (c) Synaxis. All rights reserved.
// </copyright>

using System.Threading;
using System.Threading.Tasks;
using Mediator;
using Microsoft.Extensions.Logging;
using Synaxis.Commands.Embeddings;
using Synaxis.Contracts.V1.Messages;

namespace Synaxis.Samples.Microservices;

/// <summary>
/// Embedding microservice.
/// </summary>
public class EmbeddingMicroservice
{
    private readonly IMediator mediator;
    private readonly ILogger<EmbeddingMicroservice> logger;

    public EmbeddingMicroservice(IMediator mediator, ILogger<EmbeddingMicroservice> logger)
    {
        this.mediator = mediator;
        this.logger = logger;
    }

    public async Task<EmbeddingResponse> ProcessEmbeddingAsync(EmbeddingRequestMessage request, CancellationToken cancellationToken = default)
    {
        this.logger.LogInformation("Processing embedding request: {RequestId}", request.RequestId);

        var command = new EmbeddingCommand(
            Input: new[] { request.Input },
            Model: request.Model ?? "text-embedding-ada-002");

        var response = await this.mediator.Send(command, cancellationToken);

        this.logger.LogInformation("Embedding request processed: {RequestId}", request.RequestId);

        return response;
    }
}
