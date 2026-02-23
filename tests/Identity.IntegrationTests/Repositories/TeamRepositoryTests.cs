// <copyright file="TeamRepositoryTests.cs" company="Synaxis">
// Copyright (c) Synaxis. All rights reserved.
// </copyright>

namespace Synaxis.Identity.IntegrationTests.Repositories;

using Synaxis.Identity.Domain.Aggregates;
using Synaxis.Identity.Domain.ValueObjects;
using Synaxis.Common.Tests.Time;
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

        var timeProvider = new TestTimeProvider();
        var team = Team.Create(teamId, name, description, tenantId, timeProvider);

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
        var timeProvider = new TestTimeProvider();
        var team = Team.Create(
            teamId,
            TeamName.Create("Test Team"),
            "A test team",
            Guid.NewGuid().ToString(),
            timeProvider);

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
        var timeProvider = new TestTimeProvider();
        var team = Team.Create(
            teamId,
            TeamName.Create("Test Team"),
            "A test team",
            Guid.NewGuid().ToString(),
            timeProvider);

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
        var timeProvider = new TestTimeProvider();
        var team = Team.Create(
            teamId,
            TeamName.Create("Test Team"),
            "A test team",
            Guid.NewGuid().ToString(),
            timeProvider);

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
            END;

            IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'TeamMembers')
            BEGIN
                CREATE TABLE TeamMembers (
                    TeamId NVARCHAR(100) NOT NULL,
                    UserId NVARCHAR(100) NOT NULL,
                    Role INT NOT NULL,
                    PRIMARY KEY (TeamId, UserId)
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

        // Delete existing members and insert current ones
        var deleteMembersSql = "DELETE FROM TeamMembers WHERE TeamId = @TeamId";
        using var deleteCommand = new SqlCommand(deleteMembersSql, connection);
        deleteCommand.Parameters.AddWithValue("@TeamId", team.Id);
        await deleteCommand.ExecuteNonQueryAsync();

        foreach (var member in team.Members)
        {
            var insertMemberSql = "INSERT INTO TeamMembers (TeamId, UserId, Role) VALUES (@TeamId, @UserId, @Role)";
            using var memberCommand = new SqlCommand(insertMemberSql, connection);
            memberCommand.Parameters.AddWithValue("@TeamId", team.Id);
            memberCommand.Parameters.AddWithValue("@UserId", member.Key);
            memberCommand.Parameters.AddWithValue("@Role", (int)member.Value);
            await memberCommand.ExecuteNonQueryAsync();
        }
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
            var timeProvider = new TestTimeProvider();
            var team = Team.Create(
                reader.GetString(reader.GetOrdinal("Id")),
                TeamName.Create(reader.GetString(reader.GetOrdinal("Name"))),
                reader.GetString(reader.GetOrdinal("Description")),
                reader.GetString(reader.GetOrdinal("TenantId")),
                timeProvider);

            // Load members and add them to the team using reflection
            await reader.CloseAsync();
            var memberSql = "SELECT UserId, Role FROM TeamMembers WHERE TeamId = @TeamId";
            using var memberCommand = new SqlCommand(memberSql, connection);
            memberCommand.Parameters.AddWithValue("@TeamId", teamId);
            using var memberReader = await memberCommand.ExecuteReaderAsync();

            while (await memberReader.ReadAsync())
            {
                var userId = memberReader.GetString(memberReader.GetOrdinal("UserId"));
                var role = (Role)memberReader.GetInt32(memberReader.GetOrdinal("Role"));
                team.AddMember(userId, role);
            }

            return team;
        }

        return null;
    }
}
