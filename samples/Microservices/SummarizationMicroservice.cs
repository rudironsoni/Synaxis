// <copyright file="SummarizationMicroservice.cs" company="Synaxis">
// Copyright (c) Synaxis. All rights reserved.
// </copyright>

using System.Threading;
using System.Threading.Tasks;
using Mediator;
using Microsoft.Extensions.Logging;
using Synaxis.Commands.Chat;
using Synaxis.Contracts.V1.Messages;

namespace Synaxis.Samples.Microservices;

/// <summary>
/// Summarization microservice.
/// </summary>
public class SummarizationMicroservice
{
    private readonly IMediator mediator;
    private readonly ILogger<SummarizationMicroservice> logger;

    public SummarizationMicroservice(IMediator mediator, ILogger<SummarizationMicroservice> logger)
    {
        this.mediator = mediator;
        this.logger = logger;
    }

    public async Task<ChatResponse> SummarizeAsync(SummarizationRequestMessage request, CancellationToken cancellationToken = default)
    {
        this.logger.LogInformation("Processing summarization request: {RequestId}", request.RequestId);

        var messages = new[]
        {
            new ChatMessage { Role = "system", Content = "You are a helpful assistant that summarizes text concisely." },
            new ChatMessage { Role = "user", Content = $"Please summarize the following text:\n\n{request.Text}" }
        };

        var command = new ChatCommand(
            Messages: messages,
            Model: "gpt-3.5-turbo",
            MaxTokens: request.MaxTokens ?? 200);

        var response = await this.mediator.Send(command, cancellationToken);

        this.logger.LogInformation("Summarization request processed: {RequestId}", request.RequestId);

        return response;
    }
}
