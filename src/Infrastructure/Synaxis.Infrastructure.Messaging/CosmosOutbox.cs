// <copyright file="CosmosOutbox.cs" company="Synaxis">
// Copyright (c) Synaxis. All rights reserved.
// </copyright>

namespace Synaxis.Infrastructure.Messaging;

using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Synaxis.Abstractions.Cloud;

/// <summary>
/// Cosmos DB implementation of the outbox pattern using Change Feed.
/// This is a stub implementation for future development.
/// </summary>
public class CosmosOutbox : IOutbox
{
    private readonly ILogger<CosmosOutbox> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="CosmosOutbox"/> class.
    /// </summary>
    /// <param name="logger">The logger instance.</param>
    public CosmosOutbox(ILogger<CosmosOutbox> logger)
    {
        this._logger = logger;
    }

    /// <inheritdoc/>
    public Task SaveAsync(
        IDomainEvent @event,
        CancellationToken cancellationToken = default)
    {
        this._logger.LogInformation(
            "CosmosOutbox.SaveAsync called for event {EventType} - Implementation pending",
            @event.GetType().FullName);

        return Task.CompletedTask;
    }

    /// <inheritdoc/>
    public Task<IList<OutboxMessage>> GetUnprocessedAsync(
        int batchSize = 100,
        CancellationToken cancellationToken = default)
    {
        this._logger.LogInformation(
            "CosmosOutbox.GetUnprocessedAsync called - Implementation pending");

        return Task.FromResult<IList<OutboxMessage>>(Array.Empty<OutboxMessage>());
    }

    /// <inheritdoc/>
    public Task MarkAsProcessedAsync(
        Guid messageId,
        CancellationToken cancellationToken = default)
    {
        this._logger.LogInformation(
            "CosmosOutbox.MarkAsProcessedAsync called for message {MessageId} - Implementation pending",
            messageId);

        return Task.CompletedTask;
    }

    /// <inheritdoc/>
    public Task MarkAsFailedAsync(
        Guid messageId,
        string error,
        CancellationToken cancellationToken = default)
    {
        this._logger.LogWarning(
            "CosmosOutbox.MarkAsFailedAsync called for message {MessageId}: {Error} - Implementation pending",
            messageId,
            error);

        return Task.CompletedTask;
    }
}
