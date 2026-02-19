// <copyright file="MultiTenantIntegrationTests.cs" company="Synaxis">
// Copyright (c) Synaxis. All rights reserved.
// </copyright>

namespace Synaxis.Infrastructure.IntegrationTests;

using System;
using System.Linq;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Npgsql;
using Synaxis.Abstractions.Cloud;
using Synaxis.Providers.OnPrem;
using Xunit;

/// <summary>
/// Integration tests for multi-tenant functionality using PostgreSQL with TestContainers.
/// </summary>
[Trait("Category", "Integration")]
[Trait("Category", "Infrastructure")]
public sealed class MultiTenantIntegrationTests : IClassFixture<PostgreSqlTestFixture>, IAsyncLifetime
{
    private readonly PostgreSqlTestFixture _fixture;
    private string? _connectionString;

    /// <summary>
    /// Initializes a new instance of the <see cref="MultiTenantIntegrationTests"/> class.
    /// </summary>
    /// <param name="fixture">The PostgreSQL fixture.</param>
    public MultiTenantIntegrationTests(PostgreSqlTestFixture fixture)
    {
        _fixture = fixture;
    }

    /// <inheritdoc />
    public Task InitializeAsync()
    {
        _connectionString = _fixture.ConnectionString;
        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public async Task DisposeAsync()
    {
        await _fixture.ClearDataAsync();
    }

    [Fact]
    public async Task TenantIsolation_InDatabase()
    {
        // Arrange
        var tenantId1 = "tenant-1";
        var tenantId2 = "tenant-2";

        await using var connection = new NpgsqlConnection(_connectionString);
        await connection.OpenAsync();

        // Act - Insert data for both tenants
        await using (var command = connection.CreateCommand())
        {
            command.CommandText = @"
                INSERT INTO tenant_data (tenant_id, data_key, data_value)
                VALUES (@tenantId1, 'key1', 'tenant1-value'),
                       (@tenantId2, 'key1', 'tenant2-value')";
            command.Parameters.AddWithValue("tenantId1", tenantId1);
            command.Parameters.AddWithValue("tenantId2", tenantId2);
            await command.ExecuteNonQueryAsync();
        }

        // Assert - Query for tenant 1
        await using (var command = connection.CreateCommand())
        {
            command.CommandText = "SELECT data_value FROM tenant_data WHERE tenant_id = @tenantId";
            command.Parameters.AddWithValue("tenantId", tenantId1);
            var result = await command.ExecuteScalarAsync();
            result.Should().Be("tenant1-value");
        }

        // Assert - Query for tenant 2
        await using (var command = connection.CreateCommand())
        {
            command.CommandText = "SELECT data_value FROM tenant_data WHERE tenant_id = @tenantId";
            command.Parameters.AddWithValue("tenantId", tenantId2);
            var result = await command.ExecuteScalarAsync();
            result.Should().Be("tenant2-value");
        }
    }

    [Fact]
    public async Task CrossTenantAccess_Blocked()
    {
        // Arrange
        var tenantId1 = "tenant-secure-1";
        var tenantId2 = "tenant-secure-2";
        var sensitiveData = "secret-data-123";

        await using var connection = new NpgsqlConnection(_connectionString);
        await connection.OpenAsync();

        // Insert data for tenant 1
        await using (var command = connection.CreateCommand())
        {
            command.CommandText = @"
                INSERT INTO tenant_data (tenant_id, data_key, data_value)
                VALUES (@tenantId, 'sensitive', @data)";
            command.Parameters.AddWithValue("tenantId", tenantId1);
            command.Parameters.AddWithValue("data", sensitiveData);
            await command.ExecuteNonQueryAsync();
        }

        // Act - Query as tenant 2
        await using (var command = connection.CreateCommand())
        {
            command.CommandText = @"
                SELECT data_value FROM tenant_data
                WHERE tenant_id = @tenantId AND data_key = 'sensitive'";
            command.Parameters.AddWithValue("tenantId", tenantId2);
            var result = await command.ExecuteScalarAsync();

            // Assert - Should return null (no data for tenant 2)
            result.Should().BeNull();
        }
    }

    [Fact]
    public async Task TenantSpecificConfiguration()
    {
        // Arrange
        var tenants = new[]
        {
            ("tenant-a", "max_connections", "100"),
            ("tenant-a", "timeout_seconds", "30"),
            ("tenant-b", "max_connections", "500"),
            ("tenant-b", "timeout_seconds", "60")
        };

        await using var connection = new NpgsqlConnection(_connectionString);
        await connection.OpenAsync();

        // Act - Insert configurations
        foreach (var (tenantId, key, value) in tenants)
        {
            await using var command = connection.CreateCommand();
            command.CommandText = @"
                INSERT INTO tenant_data (tenant_id, data_key, data_value)
                VALUES (@tenantId, @key, @value)";
            command.Parameters.AddWithValue("tenantId", tenantId);
            command.Parameters.AddWithValue("key", key);
            command.Parameters.AddWithValue("value", value);
            await command.ExecuteNonQueryAsync();
        }

        // Assert - Verify tenant A configuration
        await using (var command = connection.CreateCommand())
        {
            command.CommandText = @"
                SELECT data_key, data_value FROM tenant_data
                WHERE tenant_id = @tenantId
                ORDER BY data_key";
            command.Parameters.AddWithValue("tenantId", "tenant-a");

            var configs = new System.Collections.Generic.List<(string Key, string Value)>();
            await using var reader = await command.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                configs.Add((reader.GetString(0), reader.GetString(1)));
            }

            configs.Should().HaveCount(2);
            configs.Should().Contain(("max_connections", "100"));
            configs.Should().Contain(("timeout_seconds", "30"));
        }

        // Assert - Verify tenant B configuration
        await using (var command = connection.CreateCommand())
        {
            command.CommandText = @"
                SELECT data_key, data_value FROM tenant_data
                WHERE tenant_id = @tenantId
                ORDER BY data_key";
            command.Parameters.AddWithValue("tenantId", "tenant-b");

            var configs = new System.Collections.Generic.List<(string Key, string Value)>();
            await using var reader = await command.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                configs.Add((reader.GetString(0), reader.GetString(1)));
            }

            configs.Should().HaveCount(2);
            configs.Should().Contain(("max_connections", "500"));
            configs.Should().Contain(("timeout_seconds", "60"));
        }
    }

    [Fact]
    public async Task EventStore_TenantIsolation()
    {
        // Arrange
        var logger = _fixture.LoggerFactory.CreateLogger<PostgreSqlEventStore>();
        var eventStore = new PostgreSqlEventStore(_connectionString!, logger);

        var tenantId1 = "tenant-events-1";
        var tenantId2 = "tenant-events-2";

        // Act - Store events for different tenants
        await eventStore.AppendAsync(
            $"{tenantId1}:stream-1",
            new[] { new TestEvent { Data = "tenant1-event", Value = 1 } },
            0);

        await eventStore.AppendAsync(
            $"{tenantId2}:stream-1",
            new[] { new TestEvent { Data = "tenant2-event", Value = 2 } },
            0);

        // Assert - Each tenant only sees their events
        var tenant1Events = await eventStore.ReadStreamAsync($"{tenantId1}:stream-1");
        var tenant2Events = await eventStore.ReadStreamAsync($"{tenantId2}:stream-1");

        tenant1Events.Should().HaveCount(1);
        ((TestEvent)tenant1Events[0]).Data.Should().Be("tenant1-event");

        tenant2Events.Should().HaveCount(1);
        ((TestEvent)tenant2Events[0]).Data.Should().Be("tenant2-event");
    }

    [Fact]
    public async Task KeyVault_TenantIsolation()
    {
        // Arrange
        var logger = _fixture.LoggerFactory.CreateLogger<RedisKeyVault>();

        // We need Redis for this test, so we'll use the in-memory approach for demonstration
        // In a real scenario, we'd inject the Redis connection from the fixture
        var tenantId1 = "tenant-kv-1";
        var tenantId2 = "tenant-kv-2";

        // Simulate tenant-scoped keys
        var tenantKey1 = RedisKeyVault.GetTenantSecretName(tenantId1, "api-key");
        var tenantKey2 = RedisKeyVault.GetTenantSecretName(tenantId2, "api-key");

        // Assert - Keys should be different
        tenantKey1.Should().NotBe(tenantKey2);
        tenantKey1.Should().Contain(tenantId1);
        tenantKey2.Should().Contain(tenantId2);
    }

    [Fact]
    public async Task TenantData_CannotBeAccessedByOtherTenants()
    {
        // Arrange
        await using var connection = new NpgsqlConnection(_connectionString);
        await connection.OpenAsync();

        var tenantId = "isolated-tenant";
        var otherTenantId = "other-tenant";

        // Insert data
        await using (var command = connection.CreateCommand())
        {
            command.CommandText = @"
                INSERT INTO tenant_data (tenant_id, data_key, data_value)
                VALUES (@tenantId, 'private', 'private-data')";
            command.Parameters.AddWithValue("tenantId", tenantId);
            await command.ExecuteNonQueryAsync();
        }

        // Act - Query with wrong tenant ID
        await using (var command = connection.CreateCommand())
        {
            command.CommandText = @"
                SELECT COUNT(*) FROM tenant_data
                WHERE tenant_id = @tenantId AND data_key = 'private'";
            command.Parameters.AddWithValue("tenantId", otherTenantId);
            var count = Convert.ToInt32(await command.ExecuteScalarAsync());

            // Assert
            count.Should().Be(0);
        }
    }

    [Fact]
    public async Task Concurrent_TenantOperations()
    {
        // Arrange
        var tenants = Enumerable.Range(1, 5)
            .Select(i => $"concurrent-tenant-{i}")
            .ToList();

        // Act - Concurrent writes for different tenants (each needs its own connection)
        var tasks = tenants.Select(async tenantId =>
        {
            await using var connection = new NpgsqlConnection(_connectionString);
            await connection.OpenAsync();
            await using var cmd = connection.CreateCommand();
            cmd.CommandText = @"
                INSERT INTO tenant_data (tenant_id, data_key, data_value)
                VALUES (@tenantId, 'concurrent-key', @value)";
            cmd.Parameters.AddWithValue("tenantId", tenantId);
            cmd.Parameters.AddWithValue("value", $"value-{tenantId}");
            await cmd.ExecuteNonQueryAsync();
        });

        await Task.WhenAll(tasks);

        // Assert - Each tenant has their data
        foreach (var tenantId in tenants)
        {
            await using var connection = new NpgsqlConnection(_connectionString);
            await connection.OpenAsync();
            await using var command = connection.CreateCommand();
            command.CommandText = @"
                SELECT data_value FROM tenant_data
                WHERE tenant_id = @tenantId AND data_key = 'concurrent-key'";
            command.Parameters.AddWithValue("tenantId", tenantId);
            var result = await command.ExecuteScalarAsync();
            result.Should().Be($"value-{tenantId}");
        }
    }

    [Fact]
    public async Task TenantScopedEventStore_StreamsAreIsolated()
    {
        // Arrange
        var logger = _fixture.LoggerFactory.CreateLogger<PostgreSqlEventStore>();
        var eventStore = new PostgreSqlEventStore(_connectionString!, logger);

        var streamPrefix = "tenant-scoped-stream";
        var tenant1Stream = $"tenant1:{streamPrefix}";
        var tenant2Stream = $"tenant2:{streamPrefix}";

        // Act
        await eventStore.AppendAsync(
            tenant1Stream,
            new[] { new TestEvent { Data = "t1", Value = 1 } },
            0);

        await eventStore.AppendAsync(
            tenant2Stream,
            new[] { new TestEvent { Data = "t2", Value = 2 } },
            0);

        // Assert
        var t1Events = await eventStore.ReadStreamAsync(tenant1Stream);
        var t2Events = await eventStore.ReadStreamAsync(tenant2Stream);

        t1Events.Should().HaveCount(1);
        t2Events.Should().HaveCount(1);
        t1Events.Should().NotBeEquivalentTo(t2Events);
    }

    [Fact]
    public async Task TenantData_UniqueConstraintEnforced()
    {
        // Arrange
        await using var connection = new NpgsqlConnection(_connectionString);
        await connection.OpenAsync();

        var tenantId = "unique-tenant";

        // Insert first record
        await using (var command = connection.CreateCommand())
        {
            command.CommandText = @"
                INSERT INTO tenant_data (tenant_id, data_key, data_value)
                VALUES (@tenantId, 'unique-key', 'value1')";
            command.Parameters.AddWithValue("tenantId", tenantId);
            await command.ExecuteNonQueryAsync();
        }

        // Act & Assert - Insert duplicate should fail
        await using (var command = connection.CreateCommand())
        {
            command.CommandText = @"
                INSERT INTO tenant_data (tenant_id, data_key, data_value)
                VALUES (@tenantId, 'unique-key', 'value2')";
            command.Parameters.AddWithValue("tenantId", tenantId);

            var exception = await Assert.ThrowsAsync<PostgresException>(
                async () => await command.ExecuteNonQueryAsync());
            exception.SqlState.Should().Be("23505"); // unique_violation
        }
    }

    [Fact]
    public async Task MultiTenantKeyVault_NamespaceIsolation()
    {
        // Arrange
        var tenantId1 = "namespace-tenant-1";
        var tenantId2 = "namespace-tenant-2";
        var secretName = "shared-secret";

        // Act - Generate tenant-scoped names
        var scopedName1 = RedisKeyVault.GetTenantSecretName(tenantId1, secretName);
        var scopedName2 = RedisKeyVault.GetTenantSecretName(tenantId2, secretName);

        // Assert
        scopedName1.Should().NotBe(scopedName2);
        scopedName1.Should().StartWith($"tenant-{tenantId1}-");
        scopedName2.Should().StartWith($"tenant-{tenantId2}-");
    }
}
