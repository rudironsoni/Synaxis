// <copyright file="AzureServiceBus.cs" company="Synaxis">
// Copyright (c) Synaxis. All rights reserved.
// </copyright>

using System;
using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;
using global::Polly;
using MassTransit;
using Microsoft.Extensions.Logging;
using Synaxis.Abstractions.Cloud;

namespace Synaxis.Providers.Azure;

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
        _bus = bus ?? throw new ArgumentNullException(nameof(bus));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _retryPolicy = Policy
            .Handle<Exception>()
            .WaitAndRetryAsync(
                retryCount: 3,
                sleepDurationProvider: retryAttempt => TimeSpan.FromSeconds(Math.Pow(2, retryAttempt)),
                onRetry: (outcome, timespan, retryCount, context) =>
                {
                    _logger.LogWarning(
                        "Retry {RetryCount} after {Delay}s",
                        retryCount,
                        timespan.TotalSeconds);
                });
        _handlers = new ConcurrentDictionary<Type, object>();
    }

    /// <inheritdoc />
    public async Task PublishAsync<TMessage>(
        TMessage message,
        CancellationToken cancellationToken = default)
        where TMessage : class
    {
        await _retryPolicy.ExecuteAsync(async () =>
        {
            await _bus.Publish(message, cancellationToken);
            _logger.LogInformation(
                "Published message of type {MessageType}",
                typeof(TMessage).Name);
        });
    }

    /// <inheritdoc />
    public async Task PublishAsync<TMessage>(
        string topic,
        TMessage message,
        CancellationToken cancellationToken = default)
        where TMessage : class
    {
        await _retryPolicy.ExecuteAsync(async () =>
        {
            var sendEndpoint = await _bus.GetSendEndpoint(new Uri($"queue:{topic}"));
            await sendEndpoint.Send(message, cancellationToken);
            _logger.LogInformation(
                "Published message of type {MessageType} to topic {Topic}",
                typeof(TMessage).Name,
                topic);
        });
    }

    /// <inheritdoc />
    public async Task SubscribeAsync<TMessage>(
        Func<TMessage, Task> handler,
        CancellationToken cancellationToken = default)
        where TMessage : class
    {
        await _retryPolicy.ExecuteAsync(async () =>
        {
            if (_handlers.ContainsKey(typeof(TMessage)))
            {
                _logger.LogWarning(
                    "Handler for message type {MessageType} already registered",
                    typeof(TMessage).Name);
                return;
            }

            _handlers.TryAdd(typeof(TMessage), handler);

            _logger.LogInformation(
                "Subscribed to message type {MessageType}",
                typeof(TMessage).Name);

            await Task.CompletedTask;
        });
    }

    /// <inheritdoc />
    public async Task SubscribeAsync<TMessage>(
        string topic,
        Func<TMessage, Task> handler,
        CancellationToken cancellationToken = default)
        where TMessage : class
    {
        await _retryPolicy.ExecuteAsync(async () =>
        {
            var key = $"{typeof(TMessage).Name}:{topic}";

            if (_handlers.ContainsKey(typeof(TMessage)))
            {
                _logger.LogWarning(
                    "Handler for message type {MessageType} on topic {Topic} already registered",
                    typeof(TMessage).Name,
                    topic);
                return;
            }

            _handlers.TryAdd(typeof(TMessage), handler);

            _logger.LogInformation(
                "Subscribed to message type {MessageType} on topic {Topic}",
                typeof(TMessage).Name,
                topic);

            await Task.CompletedTask;
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
        if (_handlers.TryGetValue(typeof(TMessage), out var handler))
        {
            return (Func<TMessage, Task>)handler;
        }

        return null;
    }
}
