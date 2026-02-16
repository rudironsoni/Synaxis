// <copyright file="RabbitMqMessageBus.cs" company="Synaxis">
// Copyright (c) Synaxis. All rights reserved.
// </copyright>

namespace Synaxis.Providers.OnPrem;

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using Synaxis.Abstractions.Cloud;

/// <summary>
/// RabbitMQ-based implementation of IMessageBus for on-premise deployments.
/// </summary>
#pragma warning disable SA1101 // Prefix local calls with this - Fields are prefixed with underscore, not this
#pragma warning disable MA0002 // Use an overload that has a IEqualityComparer - Using default comparer for simplicity
public sealed class RabbitMqMessageBus : IMessageBus, IAsyncDisposable
{
    private IConnection? _connection;
    private IChannel? _channel;
    private readonly ILogger<RabbitMqMessageBus> _logger;
    private readonly ConcurrentDictionary<string, object> _consumers;
    private readonly string _connectionString;
    private bool _disposed;
    private bool _initialized;

    /// <summary>
    /// Initializes a new instance of the <see cref="RabbitMqMessageBus"/> class.
    /// Use <see cref="CreateAsync"/> to create and initialize an instance.
    /// </summary>
    /// <param name="connectionString">The RabbitMQ connection string.</param>
    /// <param name="logger">The logger instance.</param>
    private RabbitMqMessageBus(
        string connectionString,
        ILogger<RabbitMqMessageBus> logger)
    {
        _connectionString = connectionString;
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _consumers = new ConcurrentDictionary<string, object>(StringComparer.Ordinal);
    }

    /// <summary>
    /// Creates and initializes a new instance of the <see cref="RabbitMqMessageBus"/> class.
    /// </summary>
    /// <param name="connectionString">The RabbitMQ connection string.</param>
    /// <param name="logger">The logger instance.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    public static async Task<RabbitMqMessageBus> CreateAsync(
        string connectionString,
        ILogger<RabbitMqMessageBus> logger,
        CancellationToken cancellationToken = default)
    {
        var bus = new RabbitMqMessageBus(connectionString, logger);
        await bus.InitializeAsync(cancellationToken).ConfigureAwait(false);
        return bus;
    }

    /// <summary>
    /// Initializes the RabbitMQ connection and channel.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    private async Task InitializeAsync(CancellationToken cancellationToken)
    {
        if (_initialized)
        {
            return;
        }

        var factory = new ConnectionFactory { Uri = new Uri(_connectionString) };
        _connection = await factory.CreateConnectionAsync(cancellationToken).ConfigureAwait(false);
        _channel = await _connection.CreateChannelAsync(cancellationToken).ConfigureAwait(false);
        _initialized = true;
    }

    /// <inheritdoc />
    public Task PublishAsync<TMessage>(
        TMessage message,
        CancellationToken cancellationToken = default)
        where TMessage : class
    {
        var topic = typeof(TMessage).Name;
        return PublishAsync(topic, message, cancellationToken);
    }

    /// <inheritdoc />
    public async Task PublishAsync<TMessage>(
        string topic,
        TMessage message,
        CancellationToken cancellationToken = default)
        where TMessage : class
    {
        if (_channel == null)
        {
            throw new InvalidOperationException("RabbitMQ channel is not initialized.");
        }

        await _channel.ExchangeDeclareAsync(topic, ExchangeType.Topic, durable: true, cancellationToken: cancellationToken).ConfigureAwait(false);

        var messageJson = JsonConvert.SerializeObject(message);
        var body = Encoding.UTF8.GetBytes(messageJson);

        var properties = new BasicProperties();
        await _channel.BasicPublishAsync(topic, topic, false, properties, body, cancellationToken: cancellationToken).ConfigureAwait(false);

        _logger.LogInformation("Published message of type {MessageType} to topic {Topic}", typeof(TMessage).Name, topic);
    }

    /// <inheritdoc />
    public Task SubscribeAsync<TMessage>(
        Func<TMessage, Task> handler,
        CancellationToken cancellationToken = default)
        where TMessage : class
    {
        var topic = typeof(TMessage).Name;
        return SubscribeAsync(topic, handler, cancellationToken);
    }

    /// <inheritdoc />
    public async Task SubscribeAsync<TMessage>(
        string topic,
        Func<TMessage, Task> handler,
        CancellationToken cancellationToken = default)
        where TMessage : class
    {
        if (_channel == null)
        {
            throw new InvalidOperationException("RabbitMQ channel is not initialized.");
        }

        await _channel.ExchangeDeclareAsync(topic, ExchangeType.Topic, durable: true, cancellationToken: cancellationToken).ConfigureAwait(false);

        var queueName = $"{topic}.{Guid.NewGuid():N}";
        await _channel.QueueDeclareAsync(queueName, durable: true, exclusive: false, autoDelete: false, cancellationToken: cancellationToken).ConfigureAwait(false);
        await _channel.QueueBindAsync(queueName, topic, topic, cancellationToken: cancellationToken).ConfigureAwait(false);

        var consumer = new AsyncEventingBasicConsumer(_channel);
        consumer.ReceivedAsync += async (model, ea) =>
        {
            try
            {
                var body = ea.Body.ToArray();
                var messageJson = Encoding.UTF8.GetString(body);
                var message = JsonConvert.DeserializeObject<TMessage>(messageJson);

                if (message != null)
                {
                    await handler(message).ConfigureAwait(false);
                    await _channel.BasicAckAsync(ea.DeliveryTag, multiple: false, cancellationToken: cancellationToken).ConfigureAwait(false);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing message from topic {Topic}", topic);
                await _channel.BasicNackAsync(ea.DeliveryTag, multiple: false, requeue: true, cancellationToken: cancellationToken).ConfigureAwait(false);
            }
        };

        _ = await _channel.BasicConsumeAsync(queueName, autoAck: false, consumer: consumer, cancellationToken: cancellationToken).ConfigureAwait(false);
        _consumers.TryAdd(queueName, consumer);

        _logger.LogInformation("Subscribed to topic {Topic} on queue {Queue}", topic, queueName);
    }

    /// <summary>
    /// Disposes the RabbitMQ connection and channel asynchronously.
    /// </summary>
    public async ValueTask DisposeAsync()
    {
        if (_disposed)
        {
            return;
        }

        if (_channel != null)
        {
            await _channel.DisposeAsync().ConfigureAwait(false);
        }

        if (_connection != null)
        {
            await _connection.DisposeAsync().ConfigureAwait(false);
        }

        _disposed = true;
    }
}
#pragma warning restore MA0002 // Use an overload that has a IEqualityComparer
#pragma warning restore SA1101 // Prefix local calls with this
