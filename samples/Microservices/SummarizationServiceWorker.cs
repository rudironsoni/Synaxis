// <copyright file="SummarizationServiceWorker.cs" company="Synaxis">
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
/// Summarization service worker.
/// </summary>
public class SummarizationServiceWorker : BackgroundService
{
    private readonly SummarizationMicroservice service;
    private readonly ILogger<SummarizationServiceWorker> logger;

    public SummarizationServiceWorker(
        SummarizationMicroservice service,
        ILogger<SummarizationServiceWorker> logger)
    {
        this.service = service;
        this.logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        this.logger.LogInformation("Summarization service worker started");

        // Simulate processing requests
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                // Simulate receiving a request
                var request = new SummarizationRequestMessage
                {
                    RequestId = Guid.NewGuid().ToString(),
                    Text = "This is a long text that needs to be summarized. It contains multiple sentences and paragraphs that should be condensed into a brief summary.",
                    MaxTokens = 200
                };

                var response = await this.service.SummarizeAsync(request, stoppingToken);
                this.logger.LogInformation("Processed summarization request: {RequestId} - Summary: {Content}", request.RequestId, response.Choices[0].Message.Content);

                await Task.Delay(TimeSpan.FromSeconds(15), stoppingToken);
            }
            catch (Exception ex)
            {
                this.logger.LogError(ex, "Error processing summarization request");
            }
        }
    }
}
