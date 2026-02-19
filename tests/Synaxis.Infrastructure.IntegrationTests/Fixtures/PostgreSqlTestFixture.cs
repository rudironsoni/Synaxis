// <copyright file="PostgreSqlTestFixture.cs" company="Synaxis">
// Copyright (c) Synaxis. All rights reserved.
// </copyright>

namespace Synaxis.Infrastructure.IntegrationTests;

using System.Data;
using Microsoft.Extensions.Logging;
using Npgsql;
using Testcontainers.PostgreSql;
using Xunit;

/// <summary>
/// Fixture for PostgreSQL testing using TestContainers.
/// </summary>
public sealed class PostgreSqlTestFixture : IAsyncLifetime
{
    private readonly PostgreSqlContainer _postgresContainer;
    private readonly ILoggerFactory _loggerFactory;

    /// <summary>
    /// Initializes a new instance of the <see cref="PostgreSqlTestFixture"/> class.
    /// </summary>
    public PostgreSqlTestFixture()
    {
        _postgresContainer = new PostgreSqlBuilder()
            .WithDatabase("synaxis_test")
            .WithUsername("test")
            .WithPassword("test")
            .WithImage("postgres:16-alpine")
            .Build();

        _loggerFactory = Microsoft.Extensions.Logging.LoggerFactory.Create(builder =>
        {
            builder.AddConsole();
            builder.SetMinimumLevel(LogLevel.Information);
        });
    }

    /// <summary>
    /// Gets the connection string for the PostgreSQL container.
    /// </summary>
    public string ConnectionString { get; private set; } = string.Empty;

    /// <summary>
    /// Gets the logger factory.
    /// </summary>
    public ILoggerFactory LoggerFactory => _loggerFactory;

    /// <inheritdoc />
    public async Task InitializeAsync()
    {
        await _postgresContainer.StartAsync();
        ConnectionString = _postgresContainer.GetConnectionString();
        await InitializeDatabaseAsync();
    }

    /// <inheritdoc />
    public async Task DisposeAsync()
    {
        await _postgresContainer.DisposeAsync();
        _loggerFactory.Dispose();
    }

    /// <summary>
    /// Initializes the database schema.
    /// </summary>
    private async Task InitializeDatabaseAsync()
    {
        await using var connection = new NpgsqlConnection(ConnectionString);
        await connection.OpenAsync();

        // Create events table for event sourcing
        await using (var command = connection.CreateCommand())
        {
            command.CommandText = @"
                CREATE TABLE IF NOT EXISTS events (
                    id SERIAL PRIMARY KEY,
                    stream_id VARCHAR(255) NOT NULL,
                    version INTEGER NOT NULL,
                    event_type VARCHAR(500) NOT NULL,
                    event_data TEXT NOT NULL,
                    occurred_on TIMESTAMP NOT NULL,
                    UNIQUE(stream_id, version)
                );

                CREATE INDEX IF NOT EXISTS idx_events_stream_id ON events(stream_id);
                CREATE INDEX IF NOT EXISTS idx_events_stream_version ON events(stream_id, version);";
            await command.ExecuteNonQueryAsync();
        }

        // Create outbox table for messaging
        await using (var command = connection.CreateCommand())
        {
            command.CommandText = @"
                CREATE TABLE IF NOT EXISTS outbox_messages (
                    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
                    event_type VARCHAR(500) NOT NULL,
                    payload TEXT NOT NULL,
                    created_at TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP,
                    processed_at TIMESTAMP,
                    error TEXT,
                    retry_count INTEGER DEFAULT 0
                );

                CREATE INDEX IF NOT EXISTS idx_outbox_unprocessed ON outbox_messages(processed_at) WHERE processed_at IS NULL;
                CREATE INDEX IF NOT EXISTS idx_outbox_created ON outbox_messages(created_at);";
            await command.ExecuteNonQueryAsync();
        }

        // Create tenant isolation table
        await using (var command = connection.CreateCommand())
        {
            command.CommandText = @"
                CREATE TABLE IF NOT EXISTS tenant_data (
                    id SERIAL PRIMARY KEY,
                    tenant_id VARCHAR(255) NOT NULL,
                    data_key VARCHAR(500) NOT NULL,
                    data_value TEXT,
                    created_at TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP,
                    UNIQUE(tenant_id, data_key)
                );

                CREATE INDEX IF NOT EXISTS idx_tenant_data_tenant ON tenant_data(tenant_id);";
            await command.ExecuteNonQueryAsync();
        }
    }

    /// <summary>
    /// Clears all data from the database.
    /// </summary>
    public async Task ClearDataAsync()
    {
        await using var connection = new NpgsqlConnection(ConnectionString);
        await connection.OpenAsync();

        await using (var command = connection.CreateCommand())
        {
            command.CommandText = @"
                DO $$
                BEGIN
                    IF EXISTS (SELECT 1 FROM information_schema.tables WHERE table_name = 'events') THEN
                        TRUNCATE TABLE events RESTART IDENTITY CASCADE;
                    END IF;
                    IF EXISTS (SELECT 1 FROM information_schema.tables WHERE table_name = 'outbox_messages') THEN
                        TRUNCATE TABLE outbox_messages RESTART IDENTITY CASCADE;
                    END IF;
                    IF EXISTS (SELECT 1 FROM information_schema.tables WHERE table_name = 'tenant_data') THEN
                        TRUNCATE TABLE tenant_data RESTART IDENTITY CASCADE;
                    END IF;
                END $$;";
            await command.ExecuteNonQueryAsync();
        }
    }
}
