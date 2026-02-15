// <copyright file="OutboxProcessor.cs" company="Synaxis">
// Copyright (c) Synaxis. All rights reserved.
// </copyright>

namespace Synaxis.Infrastructure.Messaging;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Polly;
using Polly.Retry;
using Synaxis.Abstractions.Cloud;

/// <summary>
/// Processes outbox messages by publishing them to the message bus with retry logic.
/// </summary>
public class OutboxProcessor
{
    private readonly IOutbox _outbox;
    private readonly IMessageBus _messageBus;
    private readonly ILogger<OutboxProcessor> _logger;
    private readonly OutboxOptions _options;
    private readonly AsyncRetryPolicy _retryPolicy;

    /// <summary>
    /// Initializes a new instance of the <see cref="OutboxProcessor"/> class.
    /// </summary>
    /// <param name="outbox">The outbox storage.</param>
    /// <param name="messageBus">The message bus for publishing events.</param>
    /// <param name="logger">The logger instance.</param>
    /// <param name="options">The outbox configuration options.</param>
    public OutboxProcessor(
        IOutbox outbox,
        IMessageBus messageBus,
        ILogger<OutboxProcessor> logger,
        IOptions<OutboxOptions> options)
    {
        this._outbox = outbox;
        this._messageBus = messageBus;
        this._logger = logger;
        this._options = options.Value;

        this._retryPolicy = Policy
            .Handle<Exception>()
            .WaitAndRetryAsync(
                this._options.MaxRetryCount,
                retryAttempt => TimeSpan.FromSeconds(Math.Pow(2, retryAttempt)),
                onRetry: (exception, timeSpan, retryCount, context) =>
                {
                    this._logger.LogWarning(
                        exception,
                        "Retry {RetryCount} after {Delay}s for message",
                        retryCount,
                        timeSpan.TotalSeconds);
                });
    }

    /// <summary>
    /// Processes unprocessed messages from the outbox.
    /// </summary>
    /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    public async Task ProcessAsync(CancellationToken cancellationToken = default)
    {
        var messages = await this._outbox.GetUnprocessedAsync(
            this._options.BatchSize,
            cancellationToken)
            .ConfigureAwait(false);

        if (messages.Count == 0)
        {
            return;
        }

        this._logger.LogInformation("Processing {Count} outbox messages", messages.Count);

        foreach (var message in messages)
        {
            await this.ProcessMessageAsync(message, cancellationToken).ConfigureAwait(false);
        }
    }

    private async Task ProcessMessageAsync(
        OutboxMessage message,
        CancellationToken cancellationToken)
    {
        try
        {
            await this._retryPolicy.ExecuteAsync(async () =>
            {
                var eventType = Type.GetType(message.EventType);
                if (eventType == null)
                {
                    throw new InvalidOperationException(
                        $"Event type '{message.EventType}' not found");
                }

                var domainEvent = (IDomainEvent?)JsonSerializer.Deserialize(
                    message.Payload,
                    eventType);

                if (domainEvent == null)
                {
                    throw new InvalidOperationException(
                        $"Failed to deserialize event payload for message {message.Id}");
                }

                await this._messageBus.PublishAsync(domainEvent, cancellationToken)
                    .ConfigureAwait(false);
                await this._outbox.MarkAsProcessedAsync(message.Id, cancellationToken)
                    .ConfigureAwait(false);

                this._logger.LogInformation(
                    "Successfully processed outbox message {MessageId} ({EventType})",
                    message.Id,
                    message.EventType);
            }).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            this._logger.LogError(
                ex,
                "Failed to process outbox message {MessageId} after {RetryCount} attempts",
                message.Id,
                message.RetryCount + 1);

            await this._outbox.MarkAsFailedAsync(
                message.Id,
                ex.Message,
                cancellationToken)
                .ConfigureAwait(false);

            if (message.RetryCount >= this._options.MaxRetryCount)
            {
                this._logger.LogCritical(
                    "Outbox message {MessageId} exceeded max retry count and will be moved to dead letter queue",
                    message.Id);
            }
        }
    }
}
