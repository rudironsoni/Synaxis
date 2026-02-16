// <copyright file="TeamRepositoryTests.cs" company="Synaxis">
// Copyright (c) Synaxis. All rights reserved.
// </copyright>

namespace Synaxis.Identity.IntegrationTests.Repositories;

using Synaxis.Identity.Domain.Aggregates;
using Synaxis.Identity.Domain.ValueObjects;
using Testcontainers.SqlEdge;
using Xunit;
using FluentAssertions;
using Microsoft.Data.SqlClient;

[Trait("Category", "Integration")]
public class TeamRepositoryTests : IAsyncLifetime
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
    public async Task TeamRepository_CreateAndRetrieve_RoundTrip()
    {
        // Arrange
        var teamId = Guid.NewGuid().ToString();
        var name = TeamName.Create("Test Team");
        var description = "A test team";
        var tenantId = Guid.NewGuid().ToString();

        var team = Team.Create(teamId, name, description, tenantId);

        // Act
        await SaveTeamAsync(team);
        var retrievedTeam = await LoadTeamAsync(teamId);

        // Assert
        retrievedTeam.Should().NotBeNull();
        retrievedTeam!.Id.Should().Be(teamId);
        retrievedTeam.Name.Should().Be(name);
        retrievedTeam.Description.Should().Be(description);
        retrievedTeam.TenantId.Should().Be(tenantId);
    }

    [Fact]
    public async Task TeamRepository_AddMember_RoundTrip()
    {
        // Arrange
        var teamId = Guid.NewGuid().ToString();
        var team = Team.Create(
            teamId,
            TeamName.Create("Test Team"),
            "A test team",
            Guid.NewGuid().ToString());

        await SaveTeamAsync(team);

        // Act
        var userId = Guid.NewGuid().ToString();
        team.AddMember(userId, Role.Member);
        await SaveTeamAsync(team);

        var retrievedTeam = await LoadTeamAsync(teamId);

        // Assert
        retrievedTeam.Should().NotBeNull();
        retrievedTeam!.Members.Should().HaveCount(1);
        retrievedTeam.Members.Should().ContainKey(userId);
        retrievedTeam.Members[userId].Should().Be(Role.Member);
    }

    [Fact]
    public async Task TeamRepository_RemoveMember_RoundTrip()
    {
        // Arrange
        var teamId = Guid.NewGuid().ToString();
        var team = Team.Create(
            teamId,
            TeamName.Create("Test Team"),
            "A test team",
            Guid.NewGuid().ToString());

        var userId = Guid.NewGuid().ToString();
        team.AddMember(userId, Role.Member);
        await SaveTeamAsync(team);

        // Act
        team.RemoveMember(userId);
        await SaveTeamAsync(team);

        var retrievedTeam = await LoadTeamAsync(teamId);

        // Assert
        retrievedTeam.Should().NotBeNull();
        retrievedTeam!.Members.Should().BeEmpty();
    }

    [Fact]
    public async Task TeamRepository_UpdateMemberRole_RoundTrip()
    {
        // Arrange
        var teamId = Guid.NewGuid().ToString();
        var team = Team.Create(
            teamId,
            TeamName.Create("Test Team"),
            "A test team",
            Guid.NewGuid().ToString());

        var userId = Guid.NewGuid().ToString();
        team.AddMember(userId, Role.Member);
        await SaveTeamAsync(team);

        // Act
        team.UpdateMemberRole(userId, Role.Admin);
        await SaveTeamAsync(team);

        var retrievedTeam = await LoadTeamAsync(teamId);

        // Assert
        retrievedTeam.Should().NotBeNull();
        retrievedTeam!.Members[userId].Should().Be(Role.Admin);
    }

    private async Task CreateSchemaAsync()
    {
        using var connection = new SqlConnection(_connectionString);
        await connection.OpenAsync();

        var createTableSql = @"
            IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'Teams')
            BEGIN
                CREATE TABLE Teams (
                    Id NVARCHAR(100) PRIMARY KEY,
                    Name NVARCHAR(255) NOT NULL,
                    Description NVARCHAR(500) NOT NULL,
                    TenantId NVARCHAR(100) NOT NULL,
                    CreatedAt DATETIME2 NOT NULL,
                    UpdatedAt DATETIME2 NOT NULL
                );
            END";

        using var command = new SqlCommand(createTableSql, connection);
        await command.ExecuteNonQueryAsync();
    }

    private async Task SaveTeamAsync(Team team)
    {
        using var connection = new SqlConnection(_connectionString);
        await connection.OpenAsync();

        var upsertSql = @"
            MERGE Teams AS target
            USING (SELECT @Id AS Id, @Name AS Name, @Description AS Description, @TenantId AS TenantId, @CreatedAt AS CreatedAt, @UpdatedAt AS UpdatedAt) AS source
            ON (target.Id = source.Id)
            WHEN MATCHED THEN
                UPDATE SET
                    Name = source.Name,
                    Description = source.Description,
                    TenantId = source.TenantId,
                    UpdatedAt = source.UpdatedAt
            WHEN NOT MATCHED THEN
                INSERT (Id, Name, Description, TenantId, CreatedAt, UpdatedAt)
                VALUES (source.Id, source.Name, source.Description, source.TenantId, source.CreatedAt, source.UpdatedAt);";

        using var command = new SqlCommand(upsertSql, connection);
        command.Parameters.AddWithValue("@Id", team.Id);
        command.Parameters.AddWithValue("@Name", team.Name.Value);
        command.Parameters.AddWithValue("@Description", team.Description);
        command.Parameters.AddWithValue("@TenantId", team.TenantId);
        command.Parameters.AddWithValue("@CreatedAt", team.CreatedAt);
        command.Parameters.AddWithValue("@UpdatedAt", team.UpdatedAt);

        await command.ExecuteNonQueryAsync();
    }

    private async Task<Team?> LoadTeamAsync(string teamId)
    {
        using var connection = new SqlConnection(_connectionString);
        await connection.OpenAsync();

        var selectSql = "SELECT * FROM Teams WHERE Id = @Id";

        using var command = new SqlCommand(selectSql, connection);
        command.Parameters.AddWithValue("@Id", teamId);

        using var reader = await command.ExecuteReaderAsync();
        if (await reader.ReadAsync())
        {
            var team = Team.Create(
                reader.GetString(reader.GetOrdinal("Id")),
                TeamName.Create(reader.GetString(reader.GetOrdinal("Name"))),
                reader.GetString(reader.GetOrdinal("Description")),
                reader.GetString(reader.GetOrdinal("TenantId")));

            // Note: In a real implementation, we would use a repository to properly reconstruct the aggregate
            // For this test, we're just verifying the data can be stored and retrieved
            return team;
        }

        return null;
    }
}
