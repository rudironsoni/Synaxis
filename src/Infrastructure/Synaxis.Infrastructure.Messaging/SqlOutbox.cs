// <copyright file="SqlOutbox.cs" company="Synaxis">
// Copyright (c) Synaxis. All rights reserved.
// </copyright>

namespace Synaxis.Infrastructure.Messaging;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Synaxis.Abstractions.Cloud;

/// <summary>
/// SQL Server implementation of the outbox pattern using Entity Framework Core.
/// </summary>
public class SqlOutbox : IOutbox
{
    private readonly DbContext _dbContext;
    private readonly ILogger<SqlOutbox> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="SqlOutbox"/> class.
    /// </summary>
    /// <param name="dbContext">The database context.</param>
    /// <param name="logger">The logger instance.</param>
    public SqlOutbox(DbContext dbContext, ILogger<SqlOutbox> logger)
    {
        this._dbContext = dbContext;
        this._logger = logger;
    }

    /// <inheritdoc/>
    public async Task SaveAsync(
        IDomainEvent @event,
        CancellationToken cancellationToken = default)
    {
        var message = new OutboxMessage
        {
            Id = Guid.NewGuid(),
            EventType = @event.GetType().FullName!,
            Payload = JsonSerializer.Serialize(@event),
            CreatedAt = DateTime.UtcNow,
        };

        this._dbContext.Set<OutboxMessage>().Add(message);
        this._logger.LogDebug(
            "Saved outbox message {MessageId} for event {EventType}",
            message.Id,
            message.EventType);
    }

    /// <inheritdoc/>
    public async Task<IList<OutboxMessage>> GetUnprocessedAsync(
        int batchSize = 100,
        CancellationToken cancellationToken = default)
    {
        var messages = await this._dbContext.Set<OutboxMessage>()
            .Where(m => m.ProcessedAt == null)
            .OrderBy(m => m.CreatedAt)
            .Take(batchSize)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        this._logger.LogDebug(
            "Retrieved {Count} unprocessed outbox messages",
            messages.Count);

        return messages;
    }

    /// <inheritdoc/>
    public async Task MarkAsProcessedAsync(
        Guid messageId,
        CancellationToken cancellationToken = default)
    {
        var message = await this._dbContext.Set<OutboxMessage>()
            .FirstOrDefaultAsync(m => m.Id == messageId, cancellationToken)
            .ConfigureAwait(false);

        if (message != null)
        {
            message.ProcessedAt = DateTime.UtcNow;
            this._logger.LogDebug("Marked outbox message {MessageId} as processed", messageId);
        }
    }

    /// <inheritdoc/>
    public async Task MarkAsFailedAsync(
        Guid messageId,
        string error,
        CancellationToken cancellationToken = default)
    {
        var message = await this._dbContext.Set<OutboxMessage>()
            .FirstOrDefaultAsync(m => m.Id == messageId, cancellationToken)
            .ConfigureAwait(false);

        if (message != null)
        {
            message.Error = error;
            message.RetryCount++;
            this._logger.LogWarning(
                "Marked outbox message {MessageId} as failed (attempt {RetryCount}): {Error}",
                messageId,
                message.RetryCount,
                error);
        }
    }
}
