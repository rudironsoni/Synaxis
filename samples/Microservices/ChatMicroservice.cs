// <copyright file="ChatMicroservice.cs" company="Synaxis">
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
/// Chat microservice.
/// </summary>
public class ChatMicroservice
{
    private readonly IMediator mediator;
    private readonly ILogger<ChatMicroservice> logger;

    public ChatMicroservice(IMediator mediator, ILogger<ChatMicroservice> logger)
    {
        this.mediator = mediator;
        this.logger = logger;
    }

    public async Task<ChatResponse> ProcessChatAsync(ChatRequestMessage request, CancellationToken cancellationToken = default)
    {
        this.logger.LogInformation("Processing chat request: {RequestId}", request.RequestId);

        var command = new ChatCommand(
            Messages: request.Messages,
            Model: request.Model ?? "gpt-3.5-turbo",
            Temperature: request.Temperature,
            MaxTokens: request.MaxTokens);

        var response = await this.mediator.Send(command, cancellationToken);

        this.logger.LogInformation("Chat request processed: {RequestId}", request.RequestId);

        return response;
    }
}
