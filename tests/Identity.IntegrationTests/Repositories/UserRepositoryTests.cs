// <copyright file="UserRepositoryTests.cs" company="Synaxis">
// Copyright (c) Synaxis. All rights reserved.
// </copyright>

namespace Synaxis.Identity.IntegrationTests.Repositories;

using Synaxis.Common.Tests.Time;
using Synaxis.Identity.Domain.Aggregates;
using Synaxis.Identity.Domain.ValueObjects;
using Testcontainers.PostgreSql;
using Xunit;
using FluentAssertions;
using Npgsql;

[Trait("Category", "Integration")]
public class UserRepositoryTests : IAsyncLifetime
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
    public async Task UserRepository_CreateAndRetrieve_RoundTrip()
    {
        // Arrange
        var userId = Guid.NewGuid().ToString();
        var email = Email.Create("test@example.com");
        var passwordHash = PasswordHash.Create("hashedpassword");
        var firstName = "John";
        var lastName = "Doe";
        var tenantId = Guid.NewGuid().ToString();
        var timeProvider = new TestTimeProvider();

        var user = User.Create(userId, email, passwordHash, firstName, lastName, tenantId, timeProvider);

        // Act
        await SaveUserAsync(user);
        var retrievedUser = await LoadUserAsync(userId);

        // Assert
        retrievedUser.Should().NotBeNull();
        retrievedUser!.Id.Should().Be(userId);
        retrievedUser.Email.Should().Be(email);
        retrievedUser.FirstName.Should().Be(firstName);
        retrievedUser.LastName.Should().Be(lastName);
        retrievedUser.TenantId.Should().Be(tenantId);
        retrievedUser.Status.Should().Be(UserStatus.Active);
    }

    [Fact]
    public async Task UserRepository_UpdatePassword_RoundTrip()
    {
        // Arrange
        var userId = Guid.NewGuid().ToString();
        var timeProvider = new TestTimeProvider();
        var user = User.Create(
            userId,
            Email.Create("test@example.com"),
            PasswordHash.Create("oldpassword"),
            "John",
            "Doe",
            Guid.NewGuid().ToString(),
            timeProvider);

        await SaveUserAsync(user);

        // Act
        var newPasswordHash = PasswordHash.Create("newpassword");
        user.ChangePassword(newPasswordHash);
        await SaveUserAsync(user);

        var retrievedUser = await LoadUserAsync(userId);

        // Assert
        retrievedUser.Should().NotBeNull();
        retrievedUser!.PasswordHash.Should().Be(newPasswordHash);
    }

    [Fact]
    public async Task UserRepository_VerifyEmail_RoundTrip()
    {
        // Arrange
        var userId = Guid.NewGuid().ToString();
        var timeProvider = new TestTimeProvider();
        var user = User.Create(
            userId,
            Email.Create("test@example.com"),
            PasswordHash.Create("hashedpassword"),
            "John",
            "Doe",
            Guid.NewGuid().ToString(),
            timeProvider);

        await SaveUserAsync(user);

        // Act
        user.VerifyEmail();
        await SaveUserAsync(user);

        var retrievedUser = await LoadUserAsync(userId);

        // Assert
        retrievedUser.Should().NotBeNull();
        retrievedUser!.EmailVerifiedAt.Should().NotBeNull();
        retrievedUser.EmailVerifiedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(1));
    }

    [Fact]
    public async Task UserRepository_LockUser_RoundTrip()
    {
        // Arrange
        var userId = Guid.NewGuid().ToString();
        var timeProvider = new TestTimeProvider();
        var user = User.Create(
            userId,
            Email.Create("test@example.com"),
            PasswordHash.Create("hashedpassword"),
            "John",
            "Doe",
            Guid.NewGuid().ToString(),
            timeProvider);

        await SaveUserAsync(user);

        // Act
        user.Lock(TimeSpan.FromMinutes(30));
        await SaveUserAsync(user);

        var retrievedUser = await LoadUserAsync(userId);

        // Assert
        retrievedUser.Should().NotBeNull();
        retrievedUser!.LockedUntil.Should().NotBeNull();
        retrievedUser.LockedUntil.Should().BeCloseTo(DateTime.UtcNow.AddMinutes(30), TimeSpan.FromSeconds(1));
    }

    [Fact]
    public async Task UserRepository_RecordFailedLogin_RoundTrip()
    {
        // Arrange
        var userId = Guid.NewGuid().ToString();
        var timeProvider = new TestTimeProvider();
        var user = User.Create(
            userId,
            Email.Create("test@example.com"),
            PasswordHash.Create("hashedpassword"),
            "John",
            "Doe",
            Guid.NewGuid().ToString(),
            timeProvider);

        await SaveUserAsync(user);

        // Act
        user.RecordFailedLoginAttempt();
        user.RecordFailedLoginAttempt();
        await SaveUserAsync(user);

        var retrievedUser = await LoadUserAsync(userId);

        // Assert
        retrievedUser.Should().NotBeNull();
        retrievedUser!.FailedLoginAttempts.Should().Be(2);
    }

    private async Task CreateSchemaAsync()
    {
        using var connection = new NpgsqlConnection(_connectionString);
        await connection.OpenAsync();

        var createTableSql = @"
            CREATE TABLE IF NOT EXISTS Users (
                Id VARCHAR(100) PRIMARY KEY,
                Email VARCHAR(255) NOT NULL,
                PasswordHash VARCHAR(255) NOT NULL,
                FirstName VARCHAR(100) NOT NULL,
                LastName VARCHAR(100) NOT NULL,
                Status INTEGER NOT NULL,
                TenantId VARCHAR(100) NOT NULL,
                CreatedAt TIMESTAMP NOT NULL,
                UpdatedAt TIMESTAMP NOT NULL,
                EmailVerifiedAt TIMESTAMP NULL,
                LastLoginAt TIMESTAMP NULL,
                FailedLoginAttempts INTEGER NOT NULL,
                LockedUntil TIMESTAMP NULL
            );";

        using var command = new NpgsqlCommand(createTableSql, connection);
        await command.ExecuteNonQueryAsync();
    }

    private async Task SaveUserAsync(User user)
    {
        using var connection = new NpgsqlConnection(_connectionString);
        await connection.OpenAsync();

        var upsertSql = @"
            INSERT INTO Users (Id, Email, PasswordHash, FirstName, LastName, Status, TenantId, CreatedAt, UpdatedAt, EmailVerifiedAt, LastLoginAt, FailedLoginAttempts, LockedUntil)
            VALUES (@Id, @Email, @PasswordHash, @FirstName, @LastName, @Status, @TenantId, @CreatedAt, @UpdatedAt, @EmailVerifiedAt, @LastLoginAt, @FailedLoginAttempts, @LockedUntil)
            ON CONFLICT (Id) DO UPDATE SET
                Email = EXCLUDED.Email,
                PasswordHash = EXCLUDED.PasswordHash,
                FirstName = EXCLUDED.FirstName,
                LastName = EXCLUDED.LastName,
                Status = EXCLUDED.Status,
                TenantId = EXCLUDED.TenantId,
                UpdatedAt = EXCLUDED.UpdatedAt,
                EmailVerifiedAt = EXCLUDED.EmailVerifiedAt,
                LastLoginAt = EXCLUDED.LastLoginAt,
                FailedLoginAttempts = EXCLUDED.FailedLoginAttempts,
                LockedUntil = EXCLUDED.LockedUntil;";

        using var command = new NpgsqlCommand(upsertSql, connection);
        command.Parameters.AddWithValue("@Id", user.Id);
        command.Parameters.AddWithValue("@Email", user.Email.Value);
        command.Parameters.AddWithValue("@PasswordHash", user.PasswordHash.Value);
        command.Parameters.AddWithValue("@FirstName", user.FirstName);
        command.Parameters.AddWithValue("@LastName", user.LastName);
        command.Parameters.AddWithValue("@Status", (int)user.Status);
        command.Parameters.AddWithValue("@TenantId", user.TenantId);
        command.Parameters.AddWithValue("@CreatedAt", user.CreatedAt);
        command.Parameters.AddWithValue("@UpdatedAt", user.UpdatedAt);
        command.Parameters.AddWithValue("@EmailVerifiedAt", (object?)user.EmailVerifiedAt ?? DBNull.Value);
        command.Parameters.AddWithValue("@LastLoginAt", (object?)user.LastLoginAt ?? DBNull.Value);
        command.Parameters.AddWithValue("@FailedLoginAttempts", user.FailedLoginAttempts);
        command.Parameters.AddWithValue("@LockedUntil", (object?)user.LockedUntil ?? DBNull.Value);

        await command.ExecuteNonQueryAsync();
    }

    private async Task<User?> LoadUserAsync(string userId)
    {
        using var connection = new NpgsqlConnection(_connectionString);
        await connection.OpenAsync();

        var selectSql = "SELECT * FROM Users WHERE Id = @Id";

        using var command = new NpgsqlCommand(selectSql, connection);
        command.Parameters.AddWithValue("@Id", userId);

        using var reader = await command.ExecuteReaderAsync();
        if (await reader.ReadAsync())
        {
            var timeProvider = new TestTimeProvider();
            var user = User.Create(
                reader.GetString(reader.GetOrdinal("Id")),
                Email.Create(reader.GetString(reader.GetOrdinal("Email"))),
                PasswordHash.Create(reader.GetString(reader.GetOrdinal("PasswordHash"))),
                reader.GetString(reader.GetOrdinal("FirstName")),
                reader.GetString(reader.GetOrdinal("LastName")),
                reader.GetString(reader.GetOrdinal("TenantId")),
                timeProvider);

            // Update additional properties
            var status = (UserStatus)reader.GetInt32(reader.GetOrdinal("Status"));
            var emailVerifiedAt = reader.IsDBNull(reader.GetOrdinal("EmailVerifiedAt"))
                ? (DateTime?)null
                : reader.GetDateTime(reader.GetOrdinal("EmailVerifiedAt"));
            var lastLoginAt = reader.IsDBNull(reader.GetOrdinal("LastLoginAt"))
                ? (DateTime?)null
                : reader.GetDateTime(reader.GetOrdinal("LastLoginAt"));
            var failedLoginAttempts = reader.GetInt32(reader.GetOrdinal("FailedLoginAttempts"));
            var lockedUntil = reader.IsDBNull(reader.GetOrdinal("LockedUntil"))
                ? (DateTime?)null
                : reader.GetDateTime(reader.GetOrdinal("LockedUntil"));

            // Apply properties to user using reflection for testing purposes
            // These properties have private setters, so we need to use reflection with non-public binding
            var userType = user.GetType();
            var emailVerifiedAtProp = userType.GetProperty("EmailVerifiedAt", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance);
            var lastLoginAtProp = userType.GetProperty("LastLoginAt", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance);
            var failedLoginAttemptsProp = userType.GetProperty("FailedLoginAttempts", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance);
            var lockedUntilProp = userType.GetProperty("LockedUntil", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance);

            emailVerifiedAtProp?.SetValue(user, emailVerifiedAt);
            lastLoginAtProp?.SetValue(user, lastLoginAt);
            failedLoginAttemptsProp?.SetValue(user, failedLoginAttempts);
            lockedUntilProp?.SetValue(user, lockedUntil);

            return user;
        }

        return null;
    }
}
