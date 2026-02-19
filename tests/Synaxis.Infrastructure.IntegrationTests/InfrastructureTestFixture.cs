// <copyright file="InfrastructureTestFixture.cs" company="Synaxis">
// Copyright (c) Synaxis. All rights reserved.
// </copyright>

namespace Synaxis.Infrastructure.IntegrationTests;

using Xunit;

/// <summary>
/// Combined fixture for infrastructure integration tests.
/// Manages all TestContainers (PostgreSQL, Redis, RabbitMQ).
/// </summary>
public sealed class InfrastructureTestFixture : IAsyncLifetime
{
    /// <summary>
    /// Initializes a new instance of the <see cref="InfrastructureTestFixture"/> class.
    /// </summary>
    public InfrastructureTestFixture()
    {
        PostgreSql = new PostgreSqlTestFixture();
        Redis = new RedisTestFixture();
        RabbitMq = new RabbitMqTestFixture();
    }

    /// <summary>
    /// Gets the PostgreSQL test fixture.
    /// </summary>
    public PostgreSqlTestFixture PostgreSql { get; }

    /// <summary>
    /// Gets the Redis test fixture.
    /// </summary>
    public RedisTestFixture Redis { get; }

    /// <summary>
    /// Gets the RabbitMQ test fixture.
    /// </summary>
    public RabbitMqTestFixture RabbitMq { get; }

    /// <inheritdoc />
    public async Task InitializeAsync()
    {
        // Start all containers in parallel
        await Task.WhenAll(
            PostgreSql.InitializeAsync(),
            Redis.InitializeAsync(),
            RabbitMq.InitializeAsync()
        );
    }

    /// <inheritdoc />
    public async Task DisposeAsync()
    {
        // Dispose all containers
        await PostgreSql.DisposeAsync();
        await Redis.DisposeAsync();
        await RabbitMq.DisposeAsync();
    }

    /// <summary>
    /// Clears all data from all containers.
    /// </summary>
    public async Task ClearAllDataAsync()
    {
        await Task.WhenAll(
            PostgreSql.ClearDataAsync(),
            Redis.ClearDataAsync(),
            RabbitMq.PurgeQueuesAsync()
        );
    }
}
