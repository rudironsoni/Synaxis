// <copyright file="AzureServiceBus.cs" company="Synaxis">
// Copyright (c) Synaxis. All rights reserved.
// </copyright>

namespace Synaxis.Providers.Azure;

using System;
using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;
using global::Polly;
using MassTransit;
using Microsoft.Extensions.Logging;
using Synaxis.Abstractions.Cloud;

/// <summary>
/// Azure Service Bus implementation of IMessageBus using MassTransit.
/// </summary>
public class AzureServiceBus : IMessageBus
{
    private readonly IBus _bus;
    private readonly ILogger<AzureServiceBus> _logger;
    private readonly IAsyncPolicy _retryPolicy;
    private readonly ConcurrentDictionary<Type, object> _handlers;

    /// <summary>
    /// Initializes a new instance of the <see cref="AzureServiceBus"/> class.
    /// </summary>
    /// <param name="bus">The MassTransit bus instance.</param>
    /// <param name="logger">The logger instance.</param>
    public AzureServiceBus(IBus bus, ILogger<AzureServiceBus> logger)
    {
        this._bus = bus!;
        this._logger = logger!;
        this._retryPolicy = Policy
            .Handle<Exception>()
            .WaitAndRetryAsync(
                retryCount: 3,
                sleepDurationProvider: retryAttempt => TimeSpan.FromSeconds(Math.Pow(2, retryAttempt)),
                onRetry: (outcome, timespan, retryCount, context) =>
                {
                    this._logger.LogWarning(
                        "Retry {RetryCount} after {Delay}s",
                        retryCount,
                        timespan.TotalSeconds);
                });
        this._handlers = new ConcurrentDictionary<Type, object>();
    }

    /// <inheritdoc />
    public Task PublishAsync<TMessage>(
        TMessage message,
        CancellationToken cancellationToken = default)
        where TMessage : class
    {
        return this._retryPolicy.ExecuteAsync(async () =>
        {
            await this._bus.Publish(message, cancellationToken).ConfigureAwait(false);
            this._logger.LogInformation(
                "Published message of type {MessageType}",
                typeof(TMessage).Name);
        });
    }

    /// <inheritdoc />
    public Task PublishAsync<TMessage>(
        string topic,
        TMessage message,
        CancellationToken cancellationToken = default)
        where TMessage : class
    {
        return this._retryPolicy.ExecuteAsync(async () =>
        {
            var sendEndpoint = await this._bus.GetSendEndpoint(new Uri($"queue:{topic}")).ConfigureAwait(false);
            await sendEndpoint.Send(message, cancellationToken).ConfigureAwait(false);
            this._logger.LogInformation(
                "Published message of type {MessageType} to topic {Topic}",
                typeof(TMessage).Name,
                topic);
        });
    }

    /// <inheritdoc />
    public Task SubscribeAsync<TMessage>(
        Func<TMessage, Task> handler,
        CancellationToken cancellationToken = default)
        where TMessage : class
    {
        return this._retryPolicy.ExecuteAsync(async () =>
        {
            if (this._handlers.ContainsKey(typeof(TMessage)))
            {
                this._logger.LogWarning(
                    "Handler for message type {MessageType} already registered",
                    typeof(TMessage).Name);
                return;
            }

            this._handlers.TryAdd(typeof(TMessage), handler);

            this._logger.LogInformation(
                "Subscribed to message type {MessageType}",
                typeof(TMessage).Name);

            await Task.CompletedTask.ConfigureAwait(false);
        });
    }

    /// <inheritdoc />
    public Task SubscribeAsync<TMessage>(
        string topic,
        Func<TMessage, Task> handler,
        CancellationToken cancellationToken = default)
        where TMessage : class
    {
        return this._retryPolicy.ExecuteAsync(async () =>
        {
            if (this._handlers.ContainsKey(typeof(TMessage)))
            {
                this._logger.LogWarning(
                    "Handler for message type {MessageType} on topic {Topic} already registered",
                    typeof(TMessage).Name,
                    topic);
                return;
            }

            this._handlers.TryAdd(typeof(TMessage), handler);

            this._logger.LogInformation(
                "Subscribed to message type {MessageType} on topic {Topic}",
                typeof(TMessage).Name,
                topic);

            await Task.CompletedTask.ConfigureAwait(false);
        });
    }

    /// <summary>
    /// Gets the registered handler for a message type.
    /// </summary>
    /// <typeparam name="TMessage">The message type.</typeparam>
    /// <returns>The handler function, or null if not registered.</returns>
    public Func<TMessage, Task>? GetHandler<TMessage>()
        where TMessage : class
    {
        if (this._handlers.TryGetValue(typeof(TMessage), out var handler))
        {
            return (Func<TMessage, Task>)handler;
        }

        return null;
    }
}
