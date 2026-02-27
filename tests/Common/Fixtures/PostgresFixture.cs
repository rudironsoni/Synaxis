// <copyright file="PostgresFixture.cs" company="Synaxis">
// Copyright (c) Synaxis. All rights reserved.
// </copyright>

namespace Synaxis.Common.Tests.Fixtures;

using System;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Npgsql;
using Synaxis.Infrastructure.Data;
using Testcontainers.PostgreSql;
using Xunit;

/// <summary>
/// Shared PostgreSQL fixture for integration tests.
/// Manages a single PostgreSQL container for the test assembly.
/// </summary>
public sealed class PostgresFixture : IAsyncLifetime
{
    private PostgreSqlContainer? _container;

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
    /// Initializes the PostgreSQL container.
    /// </summary>
    /// <returns>A task representing the asynchronous operation.</returns>
    public async Task InitializeAsync()
    {
        this._container = new PostgreSqlBuilder("postgres:16-alpine")
            .WithDatabase("synaxis_test")
            .WithUsername("postgres")
            .WithPassword("testpassword")
            .WithCommand("-c", "max_connections=200")
            .Build();

        await this._container.StartAsync().ConfigureAwait(false);
    }

    /// <summary>
    /// Disposes the PostgreSQL container.
    /// </summary>
    /// <returns>A task representing the asynchronous operation.</returns>
    public async Task DisposeAsync()
    {
        if (this._container != null)
        {
            await this._container.DisposeAsync().ConfigureAwait(false);
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
        await admin.OpenAsync().ConfigureAwait(false);

        await using (var create = admin.CreateCommand())
        {
            create.CommandText = $"CREATE DATABASE {QuoteIdentifier(databaseName)};";
            await create.ExecuteNonQueryAsync().ConfigureAwait(false);
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
        await admin.OpenAsync().ConfigureAwait(false);

        var escapedName = databaseName.Replace("'", "''", StringComparison.Ordinal);

        await using (var terminate = admin.CreateCommand())
        {
            terminate.CommandText =
                $"SELECT pg_terminate_backend(pid) FROM pg_stat_activity WHERE datname = '{escapedName}' AND pid <> pg_backend_pid();";
            await terminate.ExecuteNonQueryAsync().ConfigureAwait(false);
        }

        await using (var drop = admin.CreateCommand())
        {
            drop.CommandText = $"DROP DATABASE IF EXISTS {QuoteIdentifier(databaseName)};";
            await drop.ExecuteNonQueryAsync().ConfigureAwait(false);
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
        await context.Database.MigrateAsync().ConfigureAwait(false);
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
        await context.Database.MigrateAsync().ConfigureAwait(false);
        return context;
    }

    /// <summary>
    /// Resets the database by dropping and recreating it.
    /// </summary>
    /// <returns>A task representing the asynchronous operation.</returns>
    public async Task ResetDatabaseAsync()
    {
        await using var context = this.CreateContext();
        await context.Database.EnsureDeletedAsync().ConfigureAwait(false);
        await context.Database.MigrateAsync().ConfigureAwait(false);
    }

    private static string QuoteIdentifier(string identifier) =>
        $"\"{identifier.Replace("\"", "\"\"", StringComparison.Ordinal)}\"";
}
