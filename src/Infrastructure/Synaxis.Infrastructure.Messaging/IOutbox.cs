// <copyright file="IOutbox.cs" company="Synaxis">
// Copyright (c) Synaxis. All rights reserved.
// </copyright>

namespace Synaxis.Infrastructure.Messaging;

using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Synaxis.Abstractions.Cloud;

/// <summary>
/// Defines a contract for storing and retrieving outbox messages.
/// </summary>
public interface IOutbox
{
    /// <summary>
    /// Saves a domain event to the outbox.
    /// </summary>
    /// <param name="event">The domain event to save.</param>
    /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    Task SaveAsync(
        IDomainEvent @event,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves unprocessed messages from the outbox.
    /// </summary>
    /// <param name="batchSize">The maximum number of messages to retrieve.</param>
    /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
    /// <returns>A task representing the asynchronous operation with the retrieved messages.</returns>
    Task<IList<OutboxMessage>> GetUnprocessedAsync(
        int batchSize = 100,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Marks a message as successfully processed.
    /// </summary>
    /// <param name="messageId">The unique identifier of the message.</param>
    /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    Task MarkAsProcessedAsync(
        Guid messageId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Marks a message as failed with an error message.
    /// </summary>
    /// <param name="messageId">The unique identifier of the message.</param>
    /// <param name="error">The error message describing the failure.</param>
    /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    Task MarkAsFailedAsync(
        Guid messageId,
        string error,
        CancellationToken cancellationToken = default);
}
