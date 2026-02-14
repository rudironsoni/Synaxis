// <copyright file="EmbeddingServiceWorker.cs" company="Synaxis">
// Copyright (c) Synaxis. All rights reserved.
// </copyright>

using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Synaxis.Contracts.V1.Messages;

namespace Synaxis.Samples.Microservices;

/// <summary>
/// Embedding service worker.
/// </summary>
public class EmbeddingServiceWorker : BackgroundService
{
    private readonly EmbeddingMicroservice service;
    private readonly ILogger<EmbeddingServiceWorker> logger;

    public EmbeddingServiceWorker(
        EmbeddingMicroservice service,
        ILogger<EmbeddingServiceWorker> logger)
    {
        this.service = service;
        this.logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        this.logger.LogInformation("Embedding service worker started");

        // Simulate processing requests
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                // Simulate receiving a request
                var request = new EmbeddingRequestMessage
                {
                    RequestId = Guid.NewGuid().ToString(),
                    Input = "Hello, world!",
                    Model = "text-embedding-ada-002"
                };

                var response = await this.service.ProcessEmbeddingAsync(request, stoppingToken);
                this.logger.LogInformation("Processed embedding request: {RequestId} - Embedding dimension: {Dimension}", request.RequestId, response.Data[0].Embedding.Length);

                await Task.Delay(TimeSpan.FromSeconds(10), stoppingToken);
            }
            catch (Exception ex)
            {
                this.logger.LogError(ex, "Error processing embedding request");
            }
        }
    }
}
