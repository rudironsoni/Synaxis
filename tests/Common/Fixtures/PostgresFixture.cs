// <copyright file="PostgresFixture.cs" company="Synaxis">
// Copyright (c) Synaxis. All rights reserved.
// </copyright>

namespace Synaxis.Common.Tests.Fixtures;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Npgsql;
using Polly;
using Synaxis.Shared.Kernel.Infrastructure.Data;
using Testcontainers.PostgreSql;
using Xunit;

/// <summary>
/// Shared PostgreSQL fixture for integration tests.
/// Manages a single PostgreSQL container for the test assembly with health checks.
/// </summary>
public sealed class PostgresFixture : IAsyncLifetime
{
    private PostgreSqlContainer? _container;
    private readonly IAsyncPolicy _retryPolicy;

    /// <summary>
    /// Initializes a new instance of the <see cref="PostgresFixture"/> class.
    /// </summary>
    public PostgresFixture()
    {
        _retryPolicy = Policy
            .Handle<NpgsqlException>()
            .WaitAndRetryAsync(
                retryCount: 3,
                sleepDurationProvider: retryAttempt =>
                    TimeSpan.FromMilliseconds(100 * Math.Pow(2, retryAttempt)));
    }

    /// <summary>
    /// Gets the connection string for the PostgreSQL container.
    /// </summary>
    public string ConnectionString => _container?.GetConnectionString()
        ?? throw new InvalidOperationException("PostgreSQL container not initialized");

    /// <summary>
    /// Gets an admin connection string targeting the postgres system database.
    /// </summary>
    public string AdminConnectionString
    {
        get
        {
            var builder = new NpgsqlConnectionStringBuilder(this.ConnectionString)
            {
                Database = "postgres",
            };

            return builder.ConnectionString;
        }
    }

    /// <summary>
    /// Gets the retry policy for database operations.
    /// </summary>
    public IAsyncPolicy RetryPolicy => _retryPolicy;

    /// <summary>
    /// Initializes the PostgreSQL container with health checks.
    /// </summary>
    /// <returns>A task representing the asynchronous operation.</returns>
    public async Task InitializeAsync()
    {
        _container = new PostgreSqlBuilder("postgres:16-alpine")
            .WithDatabase("synaxis_test")
            .WithUsername("postgres")
            .WithPassword("testpassword")
            .Build();

        await _container.StartAsync();

        // Wait for PostgreSQL to be ready with health check
        await WaitForPostgresReadyAsync();
    }

    /// <summary>
    /// Waits for PostgreSQL to be ready to accept connections.
    /// </summary>
    /// <returns>A task representing the asynchronous operation.</returns>
    private async Task WaitForPostgresReadyAsync()
    {
        var maxRetries = 30;
        var delay = TimeSpan.FromSeconds(1);

        for (int i = 0; i < maxRetries; i++)
        {
            try
            {
                await using var connection = new NpgsqlConnection(this.ConnectionString);
                await connection.OpenAsync();

                // Test that we can execute a simple query
                await using var command = connection.CreateCommand();
                command.CommandText = "SELECT 1";
                await command.ExecuteScalarAsync();

                return; // Success!
            }
            catch (Exception)
            {
                if (i == maxRetries - 1)
                {
                    throw new TimeoutException("PostgreSQL container failed to become ready within timeout.");
                }

                await Task.Delay(delay);
            }
        }
    }

    /// <summary>
    /// Disposes the PostgreSQL container.
    /// </summary>
    /// <returns>A task representing the asynchronous operation.</returns>
    public async Task DisposeAsync()
    {
        if (_container != null)
        {
            try
            {
                await _container.DisposeAsync().AsTask().WaitAsync(TimeSpan.FromMinutes(2));
            }
            catch (TimeoutException)
            {
                // Container disposal timed out, but we continue
            }
        }
    }

    /// <summary>
    /// Creates an isolated database and returns a connection string for it.
    /// </summary>
    /// <param name="namePrefix">Prefix used for generated database name.</param>
    /// <returns>Connection string pointing to the isolated database.</returns>
    public async Task<string> CreateIsolatedDatabaseAsync(string namePrefix = "synaxis_test")
    {
        var databaseName = $"{namePrefix}_{Guid.NewGuid():N}";

        await using var admin = new NpgsqlConnection(this.AdminConnectionString);
        await admin.OpenAsync();

        await using (var create = admin.CreateCommand())
        {
            create.CommandText = $"CREATE DATABASE {QuoteIdentifier(databaseName)};";
            await create.ExecuteNonQueryAsync();
        }

        var builder = new NpgsqlConnectionStringBuilder(this.ConnectionString)
        {
            Database = databaseName,
        };

        return builder.ConnectionString;
    }

    /// <summary>
    /// Drops an isolated database created for a test.
    /// </summary>
    /// <param name="connectionString">Connection string of the isolated database.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    public async Task DropDatabaseAsync(string connectionString)
    {
        var builder = new NpgsqlConnectionStringBuilder(connectionString);
        var databaseName = builder.Database;

        if (string.IsNullOrWhiteSpace(databaseName) || string.Equals(databaseName, "postgres", StringComparison.OrdinalIgnoreCase))
        {
            return;
        }

        await using var admin = new NpgsqlConnection(this.AdminConnectionString);
        await admin.OpenAsync();

        var escapedName = databaseName.Replace("'", "''", StringComparison.Ordinal);

        await using (var terminate = admin.CreateCommand())
        {
            terminate.CommandText =
                $"SELECT pg_terminate_backend(pid) FROM pg_stat_activity WHERE datname = '{escapedName}' AND pid <> pg_backend_pid();";
            await terminate.ExecuteNonQueryAsync();
        }

        await using (var drop = admin.CreateCommand())
        {
            drop.CommandText = $"DROP DATABASE IF EXISTS {QuoteIdentifier(databaseName)};";
            await drop.ExecuteNonQueryAsync();
        }
    }

    /// <summary>
    /// Creates a new DbContext with the shared connection string.
    /// </summary>
    /// <returns>A new SynaxisDbContext instance.</returns>
    public SynaxisDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<SynaxisDbContext>()
            .UseNpgsql(this.ConnectionString, npgsqlOptions => npgsqlOptions.MigrationsAssembly("Synaxis.Infrastructure"))
            .ConfigureWarnings(warnings => warnings.Ignore(RelationalEventId.PendingModelChangesWarning))
            .Options;

        return new SynaxisDbContext(options);
    }

    /// <summary>
    /// Creates a new DbContext with the provided connection string.
    /// </summary>
    /// <param name="connectionString">Connection string for target database.</param>
    /// <returns>A new SynaxisDbContext instance.</returns>
    public SynaxisDbContext CreateContext(string connectionString)
    {
        var options = new DbContextOptionsBuilder<SynaxisDbContext>()
            .UseNpgsql(connectionString, npgsqlOptions => npgsqlOptions.MigrationsAssembly("Synaxis.Infrastructure"))
            .ConfigureWarnings(warnings => warnings.Ignore(RelationalEventId.PendingModelChangesWarning))
            .Options;

        return new SynaxisDbContext(options);
    }

    /// <summary>
    /// Creates a new DbContext with the shared connection string and applies migrations.
    /// </summary>
    /// <returns>A new SynaxisDbContext instance with migrations applied.</returns>
    public async Task<SynaxisDbContext> CreateMigratedContextAsync()
    {
        var context = this.CreateContext();
        await _retryPolicy.ExecuteAsync(() => context.Database.MigrateAsync());
        return context;
    }

    /// <summary>
    /// Creates a new DbContext with the provided connection string and applies migrations.
    /// </summary>
    /// <param name="connectionString">Connection string for target database.</param>
    /// <returns>A migrated SynaxisDbContext instance.</returns>
    public async Task<SynaxisDbContext> CreateMigratedContextAsync(string connectionString)
    {
        var context = this.CreateContext(connectionString);
        await _retryPolicy.ExecuteAsync(() => context.Database.MigrateAsync());
        return context;
    }

    /// <summary>
    /// Resets the database by dropping and recreating it.
    /// </summary>
    /// <returns>A task representing the asynchronous operation.</returns>
    public async Task ResetDatabaseAsync()
    {
        await using var context = this.CreateContext();
        await _retryPolicy.ExecuteAsync(() => context.Database.EnsureDeletedAsync());
        await _retryPolicy.ExecuteAsync(() => context.Database.MigrateAsync());
    }

    private static string QuoteIdentifier(string identifier) =>
        $"\"{identifier.Replace("\"", "\"\"", StringComparison.Ordinal)}\"";
}
