// <copyright file="RedisTestFixture.cs" company="Synaxis">
// Copyright (c) Synaxis. All rights reserved.
// </copyright>

namespace Synaxis.Infrastructure.IntegrationTests;

using Microsoft.Extensions.Logging;
using StackExchange.Redis;
using Testcontainers.Redis;
using Xunit;

/// <summary>
/// Fixture for Redis testing using TestContainers.
/// </summary>
public sealed class RedisTestFixture : IAsyncLifetime
{
    private readonly RedisContainer _redisContainer;
    private readonly ILoggerFactory _loggerFactory;
    private IConnectionMultiplexer? _connectionMultiplexer;

    /// <summary>
    /// Initializes a new instance of the <see cref="RedisTestFixture"/> class.
    /// </summary>
    public RedisTestFixture()
    {
        _redisContainer = new RedisBuilder()
            .WithImage("redis:7-alpine")
            .Build();

        _loggerFactory = Microsoft.Extensions.Logging.LoggerFactory.Create(builder =>
        {
            builder.AddConsole();
            builder.SetMinimumLevel(LogLevel.Information);
        });
    }

    /// <summary>
    /// Gets the connection string for the Redis container.
    /// </summary>
    public string ConnectionString { get; private set; } = string.Empty;

    /// <summary>
    /// Gets the Redis database instance.
    /// </summary>
    public IDatabase Database => _connectionMultiplexer?.GetDatabase()
        ?? throw new InvalidOperationException("Redis connection not initialized");

    /// <summary>
    /// Gets the logger factory.
    /// </summary>
    public ILoggerFactory LoggerFactory => _loggerFactory;

    /// <inheritdoc />
    public async Task InitializeAsync()
    {
        await _redisContainer.StartAsync();
        ConnectionString = _redisContainer.GetConnectionString();
        _connectionMultiplexer = await ConnectionMultiplexer.ConnectAsync(ConnectionString);
    }

    /// <inheritdoc />
    public async Task DisposeAsync()
    {
        if (_connectionMultiplexer != null)
        {
            await _connectionMultiplexer.DisposeAsync();
        }

        await _redisContainer.DisposeAsync();
        _loggerFactory.Dispose();
    }

    /// <summary>
    /// Clears all data from Redis by deleting all keys.
    /// </summary>
    public async Task ClearDataAsync()
    {
        if (_connectionMultiplexer != null)
        {
            var db = _connectionMultiplexer.GetDatabase();
            var server = _connectionMultiplexer.GetServer(_connectionMultiplexer.GetEndPoints()[0]);

            // Get all keys and delete them (safer than FLUSHDB which requires admin mode)
            var keys = server.Keys(pattern: "*").ToArray();
            if (keys.Length > 0)
            {
                await db.KeyDeleteAsync(keys);
            }
        }
    }
}
