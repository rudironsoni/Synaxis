// <copyright file="RedisFixture.cs" company="Synaxis">
// Copyright (c) Synaxis. All rights reserved.
// </copyright>

namespace Synaxis.Common.Tests.Fixtures;

using Polly;
using StackExchange.Redis;
using Testcontainers.Redis;
using Xunit;

/// <summary>
/// Shared Redis fixture for integration tests.
/// Manages a single Redis container for the test assembly with health checks.
/// </summary>
public sealed class RedisFixture : IAsyncLifetime
{
    private RedisContainer? _container;
    private IConnectionMultiplexer? _connection;
    private readonly IAsyncPolicy _retryPolicy;

    /// <summary>
    /// Initializes a new instance of the <see cref="RedisFixture"/> class.
    /// </summary>
    public RedisFixture()
    {
        _retryPolicy = Policy
            .Handle<RedisException>()
            .WaitAndRetryAsync(
                retryCount: 3,
                sleepDurationProvider: retryAttempt =>
                    TimeSpan.FromMilliseconds(100 * Math.Pow(2, retryAttempt)));
    }

    /// <summary>
    /// Gets the connection string for the Redis container.
    /// </summary>
    public string ConnectionString => _container?.GetConnectionString()
        ?? throw new InvalidOperationException("Redis container not initialized");

    /// <summary>
    /// Gets the Redis connection multiplexer.
    /// </summary>
    public IConnectionMultiplexer Connection => _connection
        ?? throw new InvalidOperationException("Redis connection not initialized");

    /// <summary>
    /// Gets a Redis database instance.
    /// </summary>
    public IDatabase Database => Connection.GetDatabase();

    /// <summary>
    /// Gets the retry policy for Redis operations.
    /// </summary>
    public IAsyncPolicy RetryPolicy => _retryPolicy;

    /// <summary>
    /// Initializes the Redis container with health checks.
    /// </summary>
    /// <returns>A task representing the asynchronous operation.</returns>
    public async Task InitializeAsync()
    {
        _container = new RedisBuilder("redis:7-alpine").Build();

        await _container.StartAsync();

        var connectionString = _container.GetConnectionString();

        // Wait for Redis to be ready with health check
        await WaitForRedisReadyAsync(connectionString);
    }

    /// <summary>
    /// Waits for Redis to be ready to accept connections.
    /// </summary>
    /// <param name="connectionString">The Redis connection string.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    private async Task WaitForRedisReadyAsync(string connectionString)
    {
        var maxRetries = 30;
        var delay = TimeSpan.FromSeconds(1);

        for (int i = 0; i < maxRetries; i++)
        {
            try
            {
                _connection = await ConnectionMultiplexer.ConnectAsync(connectionString);

                // Test that we can execute a simple ping
                await _connection.GetDatabase().PingAsync();

                return; // Success!
            }
            catch (Exception)
            {
                if (_connection != null)
                {
                    await _connection.DisposeAsync();
                    _connection = null;
                }

                if (i == maxRetries - 1)
                {
                    throw new TimeoutException("Redis container failed to become ready within timeout.");
                }

                await Task.Delay(delay);
            }
        }
    }

    /// <summary>
    /// Disposes the Redis container and connection.
    /// </summary>
    /// <returns>A task representing the asynchronous operation.</returns>
    public async Task DisposeAsync()
    {
        if (_connection != null)
        {
            try
            {
                await _connection.CloseAsync();
                await _connection.DisposeAsync();
            }
            catch
            {
                // Ignore disposal errors during cleanup
            }
        }

        if (_container != null)
        {
            try
            {
                await _container.DisposeAsync().AsTask().WaitAsync(TimeSpan.FromMinutes(1));
            }
            catch (TimeoutException)
            {
                // Container disposal timed out, but we continue
            }
        }
    }

    /// <summary>
    /// Flushes all data from the Redis database.
    /// </summary>
    /// <returns>A task representing the asynchronous operation.</returns>
    public async Task FlushDatabaseAsync()
    {
        await _retryPolicy.ExecuteAsync(async () =>
        {
            var endpoints = Connection.GetEndPoints();
            foreach (var endpoint in endpoints)
            {
                var server = Connection.GetServer(endpoint);
                await server.FlushDatabaseAsync();
            }
        });
    }
}
