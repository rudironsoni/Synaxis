// <copyright file="UserRepositoryTests.cs" company="Synaxis">
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
public class UserRepositoryTests : IAsyncLifetime
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
    public async Task UserRepository_CreateAndRetrieve_RoundTrip()
    {
        // Arrange
        var userId = Guid.NewGuid().ToString();
        var email = Email.Create("test@example.com");
        var passwordHash = PasswordHash.Create("hashedpassword");
        var firstName = "John";
        var lastName = "Doe";
        var tenantId = Guid.NewGuid().ToString();

        var user = User.Create(userId, email, passwordHash, firstName, lastName, tenantId);

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
        var user = User.Create(
            userId,
            Email.Create("test@example.com"),
            PasswordHash.Create("oldpassword"),
            "John",
            "Doe",
            Guid.NewGuid().ToString());

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
        var user = User.Create(
            userId,
            Email.Create("test@example.com"),
            PasswordHash.Create("hashedpassword"),
            "John",
            "Doe",
            Guid.NewGuid().ToString());

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
        var user = User.Create(
            userId,
            Email.Create("test@example.com"),
            PasswordHash.Create("hashedpassword"),
            "John",
            "Doe",
            Guid.NewGuid().ToString());

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
        var user = User.Create(
            userId,
            Email.Create("test@example.com"),
            PasswordHash.Create("hashedpassword"),
            "John",
            "Doe",
            Guid.NewGuid().ToString());

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
        using var connection = new SqlConnection(_connectionString);
        await connection.OpenAsync();

        var createTableSql = @"
            IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'Users')
            BEGIN
                CREATE TABLE Users (
                    Id NVARCHAR(100) PRIMARY KEY,
                    Email NVARCHAR(255) NOT NULL,
                    PasswordHash NVARCHAR(255) NOT NULL,
                    FirstName NVARCHAR(100) NOT NULL,
                    LastName NVARCHAR(100) NOT NULL,
                    Status INT NOT NULL,
                    TenantId NVARCHAR(100) NOT NULL,
                    CreatedAt DATETIME2 NOT NULL,
                    UpdatedAt DATETIME2 NOT NULL,
                    EmailVerifiedAt DATETIME2 NULL,
                    LastLoginAt DATETIME2 NULL,
                    FailedLoginAttempts INT NOT NULL,
                    LockedUntil DATETIME2 NULL
                );
            END";

        using var command = new SqlCommand(createTableSql, connection);
        await command.ExecuteNonQueryAsync();
    }

    private async Task SaveUserAsync(User user)
    {
        using var connection = new SqlConnection(_connectionString);
        await connection.OpenAsync();

        var upsertSql = @"
            MERGE Users AS target
            USING (SELECT @Id AS Id, @Email AS Email, @PasswordHash AS PasswordHash, @FirstName AS FirstName, @LastName AS LastName, @Status AS Status, @TenantId AS TenantId, @CreatedAt AS CreatedAt, @UpdatedAt AS UpdatedAt, @EmailVerifiedAt AS EmailVerifiedAt, @LastLoginAt AS LastLoginAt, @FailedLoginAttempts AS FailedLoginAttempts, @LockedUntil AS LockedUntil) AS source
            ON (target.Id = source.Id)
            WHEN MATCHED THEN
                UPDATE SET
                    Email = source.Email,
                    PasswordHash = source.PasswordHash,
                    FirstName = source.FirstName,
                    LastName = source.LastName,
                    Status = source.Status,
                    TenantId = source.TenantId,
                    UpdatedAt = source.UpdatedAt,
                    EmailVerifiedAt = source.EmailVerifiedAt,
                    LastLoginAt = source.LastLoginAt,
                    FailedLoginAttempts = source.FailedLoginAttempts,
                    LockedUntil = source.LockedUntil
            WHEN NOT MATCHED THEN
                INSERT (Id, Email, PasswordHash, FirstName, LastName, Status, TenantId, CreatedAt, UpdatedAt, EmailVerifiedAt, LastLoginAt, FailedLoginAttempts, LockedUntil)
                VALUES (source.Id, source.Email, source.PasswordHash, source.FirstName, source.LastName, source.Status, source.TenantId, source.CreatedAt, source.UpdatedAt, source.EmailVerifiedAt, source.LastLoginAt, source.FailedLoginAttempts, source.LockedUntil);";

        using var command = new SqlCommand(upsertSql, connection);
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
        using var connection = new SqlConnection(_connectionString);
        await connection.OpenAsync();

        var selectSql = "SELECT * FROM Users WHERE Id = @Id";

        using var command = new SqlCommand(selectSql, connection);
        command.Parameters.AddWithValue("@Id", userId);

        using var reader = await command.ExecuteReaderAsync();
        if (await reader.ReadAsync())
        {
            var user = User.Create(
                reader.GetString(reader.GetOrdinal("Id")),
                Email.Create(reader.GetString(reader.GetOrdinal("Email"))),
                PasswordHash.Create(reader.GetString(reader.GetOrdinal("PasswordHash"))),
                reader.GetString(reader.GetOrdinal("FirstName")),
                reader.GetString(reader.GetOrdinal("LastName")),
                reader.GetString(reader.GetOrdinal("TenantId")));

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
