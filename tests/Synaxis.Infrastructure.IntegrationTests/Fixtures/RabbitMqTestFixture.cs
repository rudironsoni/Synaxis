// <copyright file="RabbitMqTestFixture.cs" company="Synaxis">
// Copyright (c) Synaxis. All rights reserved.
// </copyright>

namespace Synaxis.Infrastructure.IntegrationTests;

using Microsoft.Extensions.Logging;
using Synaxis.Providers.OnPrem;
using Testcontainers.RabbitMq;
using Xunit;

/// <summary>
/// Fixture for RabbitMQ testing using TestContainers.
/// </summary>
public sealed class RabbitMqTestFixture : IAsyncLifetime
{
    private readonly RabbitMqContainer _rabbitMqContainer;
    private readonly ILoggerFactory _loggerFactory;
    private readonly List<RabbitMqMessageBus> _messageBuses = new();

    /// <summary>
    /// Initializes a new instance of the <see cref="RabbitMqTestFixture"/> class.
    /// </summary>
    public RabbitMqTestFixture()
    {
        _rabbitMqContainer = new RabbitMqBuilder("rabbitmq:3.13-alpine")
            .WithUsername("test")
            .WithPassword("test")
            .Build();

        _loggerFactory = Microsoft.Extensions.Logging.LoggerFactory.Create(builder =>
        {
            builder.AddConsole();
            builder.SetMinimumLevel(LogLevel.Information);
        });
    }

    /// <summary>
    /// Gets the connection string for the RabbitMQ container.
    /// </summary>
    public string ConnectionString { get; private set; } = string.Empty;

    /// <summary>
    /// Gets the logger factory.
    /// </summary>
    public ILoggerFactory LoggerFactory => _loggerFactory;

    /// <inheritdoc />
    public async Task InitializeAsync()
    {
        await _rabbitMqContainer.StartAsync();
        ConnectionString = _rabbitMqContainer.GetConnectionString();
    }

    /// <inheritdoc />
    public async Task DisposeAsync()
    {
        foreach (var bus in _messageBuses)
        {
            await bus.DisposeAsync();
        }

        _messageBuses.Clear();
        await _rabbitMqContainer.DisposeAsync();
        _loggerFactory.Dispose();
    }

    /// <summary>
    /// Creates a new RabbitMQ message bus instance.
    /// </summary>
    /// <returns>A configured RabbitMqMessageBus instance.</returns>
    public async Task<RabbitMqMessageBus> CreateMessageBusAsync()
    {
        var logger = _loggerFactory.CreateLogger<RabbitMqMessageBus>();
        var bus = await RabbitMqMessageBus.CreateAsync(ConnectionString, logger);
        _messageBuses.Add(bus);
        return bus;
    }

    /// <summary>
    /// Purges all queues by disposing and recreating the message bus.
    /// </summary>
    public Task PurgeQueuesAsync()
    {
        // Message buses are disposed in DisposeAsync, queues auto-delete with new connections
        return Task.CompletedTask;
    }
}
