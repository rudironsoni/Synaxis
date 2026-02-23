// <copyright file="TenantRepositoryTests.cs" company="Synaxis">
// Copyright (c) Synaxis. All rights reserved.
// </copyright>

namespace Synaxis.Identity.IntegrationTests.Repositories;

using Synaxis.Common.Tests.Time;
using Synaxis.Identity.Domain.Aggregates;
using Synaxis.Identity.Domain.ValueObjects;
using Testcontainers.SqlEdge;
using Xunit;
using FluentAssertions;
using Microsoft.Data.SqlClient;

[Trait("Category", "Integration")]
public class TenantRepositoryTests : IAsyncLifetime
{
    private readonly SqlEdgeContainer _sqlContainer = new SqlEdgeBuilder()
        .WithImage("mcr.microsoft.com/azure-sql-edge:latest")
        .WithPassword("YourStrong@Passw0rd")
        .Build();

    private string _connectionString = null!;

    public async Task InitializeAsync()
    {
        await _sqlContainer.StartAsync();
        _connectionString = _sqlContainer.GetConnectionString();

        // Create database schema
        await CreateSchemaAsync();
    }

    public async Task DisposeAsync()
    {
        await _sqlContainer.DisposeAsync();
    }

    [Fact]
    public async Task TenantRepository_CreateAndRetrieve_RoundTrip()
    {
        // Arrange
        var tenantId = Guid.NewGuid().ToString();
        var name = TenantName.Create("Test Tenant");
        var slug = "test-tenant";
        var primaryRegion = "eastus";
        var timeProvider = new TestTimeProvider();

        var tenant = Tenant.Provision(tenantId, name, slug, primaryRegion, timeProvider);

        // Act
        await SaveTenantAsync(tenant);
        var retrievedTenant = await LoadTenantAsync(tenantId);

        // Assert
        retrievedTenant.Should().NotBeNull();
        retrievedTenant!.Id.Should().Be(tenantId);
        retrievedTenant.Name.Should().Be(name);
        retrievedTenant.Slug.Should().Be(slug);
        retrievedTenant.PrimaryRegion.Should().Be(primaryRegion);
        retrievedTenant.Status.Should().Be(TenantStatus.Active);
    }

    [Fact]
    public async Task TenantRepository_UpdateSettings_RoundTrip()
    {
        // Arrange
        var tenantId = Guid.NewGuid().ToString();
        var timeProvider = new TestTimeProvider();
        var tenant = Tenant.Provision(
            tenantId,
            TenantName.Create("Test Tenant"),
            "test-tenant",
            "eastus",
            timeProvider);

        await SaveTenantAsync(tenant);

        // Act
        var newSettings = new Dictionary<string, string>
        {
            ["setting1"] = "value1",
            ["setting2"] = "value2"
        };
        tenant.UpdateSettings(newSettings);
        await SaveTenantAsync(tenant);

        var retrievedTenant = await LoadTenantAsync(tenantId);

        // Assert
        retrievedTenant.Should().NotBeNull();
        retrievedTenant!.Settings.Should().BeEquivalentTo(newSettings);
    }

    [Fact]
    public async Task TenantRepository_Suspend_RoundTrip()
    {
        // Arrange
        var tenantId = Guid.NewGuid().ToString();
        var timeProvider = new TestTimeProvider();
        var tenant = Tenant.Provision(
            tenantId,
            TenantName.Create("Test Tenant"),
            "test-tenant",
            "eastus",
            timeProvider);

        await SaveTenantAsync(tenant);

        // Act
        tenant.Suspend();
        await SaveTenantAsync(tenant);

        var retrievedTenant = await LoadTenantAsync(tenantId);

        // Assert
        retrievedTenant.Should().NotBeNull();
        retrievedTenant!.Status.Should().Be(TenantStatus.Suspended);
    }

    [Fact]
    public async Task TenantRepository_Activate_RoundTrip()
    {
        // Arrange
        var tenantId = Guid.NewGuid().ToString();
        var timeProvider = new TestTimeProvider();
        var tenant = Tenant.Provision(
            tenantId,
            TenantName.Create("Test Tenant"),
            "test-tenant",
            "eastus",
            timeProvider);

        tenant.Suspend();
        await SaveTenantAsync(tenant);

        // Act
        tenant.Activate();
        await SaveTenantAsync(tenant);

        var retrievedTenant = await LoadTenantAsync(tenantId);

        // Assert
        retrievedTenant.Should().NotBeNull();
        retrievedTenant!.Status.Should().Be(TenantStatus.Active);
    }

    private async Task CreateSchemaAsync()
    {
        using var connection = new SqlConnection(_connectionString);
        await connection.OpenAsync();

        var createTableSql = @"
            IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'Tenants')
            BEGIN
                CREATE TABLE Tenants (
                    Id NVARCHAR(100) PRIMARY KEY,
                    Name NVARCHAR(255) NOT NULL,
                    Slug NVARCHAR(100) NOT NULL,
                    Status INT NOT NULL,
                    PrimaryRegion NVARCHAR(50) NOT NULL,
                    CreatedAt DATETIME2 NOT NULL,
                    UpdatedAt DATETIME2 NOT NULL
                );
            END;

            IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'TenantSettings')
            BEGIN
                CREATE TABLE TenantSettings (
                    TenantId NVARCHAR(100) NOT NULL,
                    SettingKey NVARCHAR(100) NOT NULL,
                    SettingValue NVARCHAR(500) NOT NULL,
                    PRIMARY KEY (TenantId, SettingKey)
                );
            END";

        using var command = new SqlCommand(createTableSql, connection);
        await command.ExecuteNonQueryAsync();
    }

    private async Task SaveTenantAsync(Tenant tenant)
    {
        using var connection = new SqlConnection(_connectionString);
        await connection.OpenAsync();

        var upsertSql = @"
            MERGE Tenants AS target
            USING (SELECT @Id AS Id, @Name AS Name, @Slug AS Slug, @Status AS Status, @PrimaryRegion AS PrimaryRegion, @CreatedAt AS CreatedAt, @UpdatedAt AS UpdatedAt) AS source
            ON (target.Id = source.Id)
            WHEN MATCHED THEN
                UPDATE SET
                    Name = source.Name,
                    Slug = source.Slug,
                    Status = source.Status,
                    PrimaryRegion = source.PrimaryRegion,
                    UpdatedAt = source.UpdatedAt
            WHEN NOT MATCHED THEN
                INSERT (Id, Name, Slug, Status, PrimaryRegion, CreatedAt, UpdatedAt)
                VALUES (source.Id, source.Name, source.Slug, source.Status, source.PrimaryRegion, source.CreatedAt, source.UpdatedAt);";

        using var command = new SqlCommand(upsertSql, connection);
        command.Parameters.AddWithValue("@Id", tenant.Id);
        command.Parameters.AddWithValue("@Name", tenant.Name.Value);
        command.Parameters.AddWithValue("@Slug", tenant.Slug);
        command.Parameters.AddWithValue("@Status", (int)tenant.Status);
        command.Parameters.AddWithValue("@PrimaryRegion", tenant.PrimaryRegion);
        command.Parameters.AddWithValue("@CreatedAt", tenant.CreatedAt);
        command.Parameters.AddWithValue("@UpdatedAt", tenant.UpdatedAt);

        await command.ExecuteNonQueryAsync();

        // Delete existing settings and insert current ones
        var deleteSettingsSql = "DELETE FROM TenantSettings WHERE TenantId = @TenantId";
        using var deleteCommand = new SqlCommand(deleteSettingsSql, connection);
        deleteCommand.Parameters.AddWithValue("@TenantId", tenant.Id);
        await deleteCommand.ExecuteNonQueryAsync();

        foreach (var setting in tenant.Settings)
        {
            var insertSettingSql = "INSERT INTO TenantSettings (TenantId, SettingKey, SettingValue) VALUES (@TenantId, @SettingKey, @SettingValue)";
            using var settingCommand = new SqlCommand(insertSettingSql, connection);
            settingCommand.Parameters.AddWithValue("@TenantId", tenant.Id);
            settingCommand.Parameters.AddWithValue("@SettingKey", setting.Key);
            settingCommand.Parameters.AddWithValue("@SettingValue", setting.Value);
            await settingCommand.ExecuteNonQueryAsync();
        }
    }

    private async Task<Tenant?> LoadTenantAsync(string tenantId)
    {
        using var connection = new SqlConnection(_connectionString);
        await connection.OpenAsync();

        var selectSql = "SELECT * FROM Tenants WHERE Id = @Id";

        using var command = new SqlCommand(selectSql, connection);
        command.Parameters.AddWithValue("@Id", tenantId);

        using var reader = await command.ExecuteReaderAsync();
        if (await reader.ReadAsync())
        {
            var timeProvider = new TestTimeProvider();
            var tenant = Tenant.Provision(
                reader.GetString(reader.GetOrdinal("Id")),
                TenantName.Create(reader.GetString(reader.GetOrdinal("Name"))),
                reader.GetString(reader.GetOrdinal("Slug")),
                reader.GetString(reader.GetOrdinal("PrimaryRegion")),
                timeProvider);

            // Set Status using reflection
            var status = (TenantStatus)reader.GetInt32(reader.GetOrdinal("Status"));
            var tenantType = tenant.GetType();
            tenantType.GetProperty("Status", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance)
                ?.SetValue(tenant, status);

            // Load settings
            await reader.CloseAsync();
            var settingsSql = "SELECT SettingKey, SettingValue FROM TenantSettings WHERE TenantId = @TenantId";
            using var settingsCommand = new SqlCommand(settingsSql, connection);
            settingsCommand.Parameters.AddWithValue("@TenantId", tenantId);
            using var settingsReader = await settingsCommand.ExecuteReaderAsync();

            var settings = new Dictionary<string, string>();
            while (await settingsReader.ReadAsync())
            {
                var key = settingsReader.GetString(settingsReader.GetOrdinal("SettingKey"));
                var value = settingsReader.GetString(settingsReader.GetOrdinal("SettingValue"));
                settings[key] = value;
            }

            if (settings.Count > 0)
            {
                tenantType.GetProperty("Settings", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance)
                    ?.SetValue(tenant, settings);
            }

            return tenant;
        }

        return null;
    }
}
