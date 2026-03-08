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
using Synaxis.Shared.Kernel.Application.Cloud;

/// <summary>
/// RabbitMQ-based implementation of IMessageBus for on-premise deployments.
/// </summary>
public sealed class RabbitMqMessageBus : IMessageBus, IAsyncDisposable
{
    private readonly ILogger<RabbitMqMessageBus> _logger;
    private readonly ConcurrentDictionary<string, object> _consumers;
    private readonly string _connectionString;
    private IConnection? _connection;
    private IChannel? _channel;
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
        this._connectionString = connectionString;
        ArgumentNullException.ThrowIfNull(logger);
        this._logger = logger;
        this._consumers = new ConcurrentDictionary<string, object>(StringComparer.Ordinal);
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
        if (this._initialized)
        {
            return;
        }

        var factory = new ConnectionFactory { Uri = new Uri(this._connectionString) };

        // Dispose any existing connection/channel before creating new ones
        if (this._channel != null)
        {
            await this._channel.DisposeAsync().ConfigureAwait(false);
        }

        if (this._connection != null)
        {
            await this._connection.DisposeAsync().ConfigureAwait(false);
        }

        this._connection = await factory.CreateConnectionAsync(cancellationToken).ConfigureAwait(false);
        this._channel = await this._connection.CreateChannelAsync(new CreateChannelOptions(false, false), cancellationToken).ConfigureAwait(false);
        this._initialized = true;
    }

    /// <inheritdoc />
    public Task PublishAsync<TMessage>(
        TMessage message,
        CancellationToken cancellationToken = default)
        where TMessage : class
    {
        var topic = typeof(TMessage).Name;
        return this.PublishAsync(topic, message, cancellationToken);
    }

    /// <inheritdoc />
    public async Task PublishAsync<TMessage>(
        string topic,
        TMessage message,
        CancellationToken cancellationToken = default)
        where TMessage : class
    {
        if (this._channel == null)
        {
            throw new InvalidOperationException("RabbitMQ channel is not initialized.");
        }

        await this._channel.ExchangeDeclareAsync(topic, ExchangeType.Topic, durable: true, cancellationToken: cancellationToken).ConfigureAwait(false);

        var messageJson = JsonConvert.SerializeObject(message);
        var body = Encoding.UTF8.GetBytes(messageJson);

        var properties = new BasicProperties();
        await this._channel.BasicPublishAsync(topic, topic, false, properties, body, cancellationToken: cancellationToken).ConfigureAwait(false);

        this._logger.LogInformation("Published message of type {MessageType} to topic {Topic}", typeof(TMessage).Name, topic);
    }

    /// <inheritdoc />
    public Task SubscribeAsync<TMessage>(
        Func<TMessage, Task> handler,
        CancellationToken cancellationToken = default)
        where TMessage : class
    {
        var topic = typeof(TMessage).Name;
        return this.SubscribeAsync(topic, handler, cancellationToken);
    }

    /// <inheritdoc />
    public async Task SubscribeAsync<TMessage>(
        string topic,
        Func<TMessage, Task> handler,
        CancellationToken cancellationToken = default)
        where TMessage : class
    {
        if (this._channel == null)
        {
            throw new InvalidOperationException("RabbitMQ channel is not initialized.");
        }

        await this._channel.ExchangeDeclareAsync(topic, ExchangeType.Topic, durable: true, cancellationToken: cancellationToken).ConfigureAwait(false);

        var queueName = $"{topic}.{Guid.NewGuid():N}";
        await this._channel.QueueDeclareAsync(queueName, durable: true, exclusive: false, autoDelete: false, cancellationToken: cancellationToken).ConfigureAwait(false);
        await this._channel.QueueBindAsync(queueName, topic, topic, cancellationToken: cancellationToken).ConfigureAwait(false);

        var consumer = new AsyncEventingBasicConsumer(this._channel);
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
                    await this._channel.BasicAckAsync(ea.DeliveryTag, multiple: false, cancellationToken: cancellationToken).ConfigureAwait(false);
                }
            }
            catch (Exception ex)
            {
                this._logger.LogError(ex, "Error processing message from topic {Topic}", topic);
                await this._channel.BasicNackAsync(ea.DeliveryTag, multiple: false, requeue: true, cancellationToken: cancellationToken).ConfigureAwait(false);
            }
        };

        _ = await this._channel.BasicConsumeAsync(queueName, autoAck: false, consumer: consumer, cancellationToken: cancellationToken).ConfigureAwait(false);
        this._consumers.TryAdd(queueName, consumer);

        this._logger.LogInformation("Subscribed to topic {Topic} on queue {Queue}", topic, queueName);
    }

    /// <summary>
    /// Disposes the RabbitMQ connection and channel asynchronously.
    /// </summary>
    /// <returns>A <see cref="ValueTask"/> representing the asynchronous operation.</returns>
    public async ValueTask DisposeAsync()
    {
        if (this._disposed)
        {
            return;
        }

        if (this._channel != null)
        {
            await this._channel.DisposeAsync().ConfigureAwait(false);
        }

        if (this._connection != null)
        {
            await this._connection.DisposeAsync().ConfigureAwait(false);
        }

        this._disposed = true;
    }
}
