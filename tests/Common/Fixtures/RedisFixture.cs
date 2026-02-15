// <copyright file="RedisFixture.cs" company="Synaxis">
// Copyright (c) Synaxis. All rights reserved.
// </copyright>

namespace Synaxis.Common.Tests.Fixtures;

using System;
using System.Threading.Tasks;
using StackExchange.Redis;
using Testcontainers.Redis;
using Xunit;

#pragma warning disable IDISP003 // False positive: fields are only assigned once in InitializeAsync

/// <summary>
/// Shared Redis fixture for integration tests.
/// Manages a single Redis container for the test assembly.
/// </summary>
public sealed class RedisFixture : IAsyncLifetime, IDisposable
{
    private RedisContainer? _container;
    private IConnectionMultiplexer? _connection;

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
    /// Initializes the Redis container.
    /// </summary>
    /// <returns>A task representing the asynchronous operation.</returns>
    public async Task InitializeAsync()
    {
        _container = new RedisBuilder("redis:7-alpine")
            .WithCommand("--protected-mode", "no")
            .Build();

        await _container.StartAsync();

        var connectionString = _container.GetConnectionString();
        _connection = await ConnectionMultiplexer.ConnectAsync(connectionString);
    }

    /// <summary>
    /// Disposes the Redis container and connection.
    /// </summary>
    /// <returns>A task representing the asynchronous operation.</returns>
    public async Task DisposeAsync()
    {
        if (_container != null)
        {
            await _container.DisposeAsync();
        }
    }

    /// <summary>
    /// Disposes the Redis connection.
    /// </summary>
    public void Dispose()
    {
        if (_connection != null)
        {
            _connection.CloseAsync().GetAwaiter().GetResult();
            _connection.Dispose();
        }
    }

    /// <summary>
    /// Flushes all data from the Redis database.
    /// </summary>
    /// <returns>A task representing the asynchronous operation.</returns>
    public async Task FlushDatabaseAsync()
    {
        var endpoints = Connection.GetEndPoints();
        foreach (var endpoint in endpoints)
        {
            var server = Connection.GetServer(endpoint);
            await server.FlushDatabaseAsync();
        }
    }
}
