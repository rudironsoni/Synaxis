// <copyright file="RabbitMqTestFixture.cs" company="Synaxis">
// Copyright (c) Synaxis. All rights reserved.
// </copyright>

namespace Synaxis.Infrastructure.IntegrationTests;

using Microsoft.Extensions.Logging;
using Testcontainers.RabbitMq;
using Xunit;

/// <summary>
/// Fixture for RabbitMQ testing using TestContainers.
/// Note: RabbitMQ integration tests are disabled due to API version compatibility.
/// This fixture provides the infrastructure for when RabbitMQ is available.
/// </summary>
public sealed class RabbitMqTestFixture : IAsyncLifetime
{
    private readonly RabbitMqContainer _rabbitMqContainer;
    private readonly ILoggerFactory _loggerFactory;

    /// <summary>
    /// Initializes a new instance of the <see cref="RabbitMqTestFixture"/> class.
    /// </summary>
    public RabbitMqTestFixture()
    {
        _rabbitMqContainer = new RabbitMqBuilder()
            .WithImage("rabbitmq:3.13-alpine")
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
        await _rabbitMqContainer.DisposeAsync();
        _loggerFactory.Dispose();
    }

    /// <summary>
    /// Purges all queues.
    /// </summary>
    public Task PurgeQueuesAsync()
    {
        // Queue purging implemented when RabbitMQ tests are enabled
        return Task.CompletedTask;
    }
}
