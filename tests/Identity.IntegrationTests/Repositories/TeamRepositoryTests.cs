// <copyright file="TeamRepositoryTests.cs" company="Synaxis">
// Copyright (c) Synaxis. All rights reserved.
// </copyright>

namespace Synaxis.Identity.IntegrationTests.Repositories;

using Synaxis.Identity.Domain.Aggregates;
using Synaxis.Identity.Domain.ValueObjects;
using Synaxis.Common.Tests.Time;
using Testcontainers.PostgreSql;
using Xunit;
using FluentAssertions;
using Npgsql;

[Trait("Category", "Integration")]
public class TeamRepositoryTests : IAsyncLifetime
{
    private readonly PostgreSqlContainer _sqlContainer = new PostgreSqlBuilder("postgres:16")
        .WithDatabase("testdb")
        .WithUsername("testuser")
        .WithPassword("testpass")
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
        using var connection = new NpgsqlConnection(_connectionString);
        await connection.OpenAsync();

        var createTableSql = @"
            CREATE TABLE IF NOT EXISTS Teams (
                Id VARCHAR(100) PRIMARY KEY,
                Name VARCHAR(255) NOT NULL,
                Description VARCHAR(500) NOT NULL,
                TenantId VARCHAR(100) NOT NULL,
                CreatedAt TIMESTAMP NOT NULL,
                UpdatedAt TIMESTAMP NOT NULL
            );

            CREATE TABLE IF NOT EXISTS TeamMembers (
                TeamId VARCHAR(100) NOT NULL,
                UserId VARCHAR(100) NOT NULL,
                Role INTEGER NOT NULL,
                PRIMARY KEY (TeamId, UserId)
            );";

        using var command = new NpgsqlCommand(createTableSql, connection);
        await command.ExecuteNonQueryAsync();
    }

    private async Task SaveTeamAsync(Team team)
    {
        using var connection = new NpgsqlConnection(_connectionString);
        await connection.OpenAsync();

        var upsertSql = @"
            INSERT INTO Teams (Id, Name, Description, TenantId, CreatedAt, UpdatedAt)
            VALUES (@Id, @Name, @Description, @TenantId, @CreatedAt, @UpdatedAt)
            ON CONFLICT (Id) DO UPDATE SET
                Name = EXCLUDED.Name,
                Description = EXCLUDED.Description,
                TenantId = EXCLUDED.TenantId,
                UpdatedAt = EXCLUDED.UpdatedAt;";

        using var command = new NpgsqlCommand(upsertSql, connection);
        command.Parameters.AddWithValue("@Id", team.Id);
        command.Parameters.AddWithValue("@Name", team.Name.Value);
        command.Parameters.AddWithValue("@Description", team.Description);
        command.Parameters.AddWithValue("@TenantId", team.TenantId);
        command.Parameters.AddWithValue("@CreatedAt", team.CreatedAt);
        command.Parameters.AddWithValue("@UpdatedAt", team.UpdatedAt);

        await command.ExecuteNonQueryAsync();

        // Delete existing members and insert current ones
        var deleteMembersSql = "DELETE FROM TeamMembers WHERE TeamId = @TeamId";
        using var deleteCommand = new NpgsqlCommand(deleteMembersSql, connection);
        deleteCommand.Parameters.AddWithValue("@TeamId", team.Id);
        await deleteCommand.ExecuteNonQueryAsync();

        foreach (var member in team.Members)
        {
            var insertMemberSql = "INSERT INTO TeamMembers (TeamId, UserId, Role) VALUES (@TeamId, @UserId, @Role)";
            using var memberCommand = new NpgsqlCommand(insertMemberSql, connection);
            memberCommand.Parameters.AddWithValue("@TeamId", team.Id);
            memberCommand.Parameters.AddWithValue("@UserId", member.Key);
            memberCommand.Parameters.AddWithValue("@Role", (int)member.Value);
            await memberCommand.ExecuteNonQueryAsync();
        }
    }

    private async Task<Team?> LoadTeamAsync(string teamId)
    {
        using var connection = new NpgsqlConnection(_connectionString);
        await connection.OpenAsync();

        var selectSql = "SELECT * FROM Teams WHERE Id = @Id";

        using var command = new NpgsqlCommand(selectSql, connection);
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
            using var memberCommand = new NpgsqlCommand(memberSql, connection);
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
