// <copyright file="BillingDatabaseFixture.cs" company="Synaxis">
// Copyright (c) Synaxis. All rights reserved.
// </copyright>

namespace Synaxis.Billing.IntegrationTests.Fixtures;

using global::Billing.Infrastructure.Data;
#nullable enable
using Microsoft.EntityFrameworkCore;
using Npgsql;
using Respawn;
using Testcontainers.PostgreSql;
using Xunit;

/// <summary>
/// Shared database fixture for Billing integration tests using PostgreSQL container and Respawn.
/// </summary>
public sealed class BillingDatabaseFixture : IAsyncLifetime
{
    private readonly PostgreSqlContainer _postgresContainer;
    private Respawner? _respawner;
    private string _connectionString = null!;

    /// <summary>
    /// Initializes a new instance of the <see cref="BillingDatabaseFixture"/> class.
    /// </summary>
    public BillingDatabaseFixture()
    {
        this._postgresContainer = new PostgreSqlBuilder("postgres:16-alpine")
            .WithDatabase("billing_test")
            .WithUsername("postgres")
            .WithPassword("testpassword")
            .WithCommand("-c", "max_connections=200")
            .Build();
    }

    /// <summary>
    /// Gets the connection string for the PostgreSQL container.
    /// </summary>
    public string ConnectionString => this._connectionString;

    /// <inheritdoc />
    public async Task InitializeAsync()
    {
        await this._postgresContainer.StartAsync();
        this._connectionString = this._postgresContainer.GetConnectionString();

        // Apply migrations
        await ApplyMigrationsAsync();

        // Initialize Respawn for database reset using NpgsqlConnection
        await using var connection = new NpgsqlConnection(this._connectionString);
        await connection.OpenAsync();
        this._respawner = await Respawner.CreateAsync(
            connection,
            new RespawnerOptions
            {
                DbAdapter = DbAdapter.Postgres,
                TablesToIgnore = ["__EFMigrationsHistory"],
            });
    }

    /// <inheritdoc />
    public async Task DisposeAsync()
    {
        await this._postgresContainer.DisposeAsync();
    }

    /// <summary>
    /// Resets the database to a clean state using Respawn.
    /// </summary>
    public async Task ResetDatabaseAsync()
    {
        if (this._respawner == null)
        {
            throw new InvalidOperationException("Respawner not initialized");
        }

        await using var connection = new NpgsqlConnection(this._connectionString);
        await connection.OpenAsync();
        await this._respawner.ResetAsync(connection);
    }

    /// <summary>
    /// Creates a new DbContext instance.
    /// </summary>
    public BillingDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<BillingDbContext>()
            .UseNpgsql(this._connectionString)
            .Options;

        return new BillingDbContext(options);
    }

    private async Task ApplyMigrationsAsync()
    {
        await using var context = this.CreateDbContext();
        await context.Database.EnsureCreatedAsync();
    }
}

/// <summary>
/// Collection fixture for Billing integration tests.
/// </summary>
[CollectionDefinition("BillingIntegration")]
public sealed class BillingIntegrationCollection : ICollectionFixture<BillingDatabaseFixture>
{
    // This class has no code, and is never created. Its purpose is simply
    // to be the place to apply [CollectionDefinition] and all the ICollectionFixture<> interfaces.
}
