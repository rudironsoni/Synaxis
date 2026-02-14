// <copyright file="ChatServiceWorker.cs" company="Synaxis">
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
/// Chat service worker.
/// </summary>
public class ChatServiceWorker : BackgroundService
{
    private readonly ChatMicroservice service;
    private readonly ILogger<ChatServiceWorker> logger;

    public ChatServiceWorker(
        ChatMicroservice service,
        ILogger<ChatServiceWorker> logger)
    {
        this.service = service;
        this.logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        this.logger.LogInformation("Chat service worker started");

        // Simulate processing requests
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                // Simulate receiving a request
                var request = new ChatRequestMessage
                {
                    RequestId = Guid.NewGuid().ToString(),
                    Messages = new[]
                    {
                        new ChatMessage { Role = "user", Content = "Hello!" }
                    },
                    Model = "gpt-3.5-turbo"
                };

                var response = await this.service.ProcessChatAsync(request, stoppingToken);
                this.logger.LogInformation("Processed chat request: {RequestId} - Response: {Content}", request.RequestId, response.Choices[0].Message.Content);

                await Task.Delay(TimeSpan.FromSeconds(5), stoppingToken);
            }
            catch (Exception ex)
            {
                this.logger.LogError(ex, "Error processing chat request");
            }
        }
    }
}
