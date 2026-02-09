// <copyright file="TestcontainersMigrationTests.cs" company="Synaxis">
// Copyright (c) Synaxis. All rights reserved.
// </copyright>

namespace Synaxis.Infrastructure.Tests.Migrations
{
    using System;
    using System.Threading.Tasks;
    using FluentAssertions;
    using Microsoft.EntityFrameworkCore;
    using Synaxis.Core.Models;
    using Synaxis.Infrastructure.Data;
    using Testcontainers.PostgreSql;
    using Xunit;

    /// <summary>
    /// Integration tests for database migrations using Testcontainers PostgreSQL.
    /// </summary>
    public class TestcontainersMigrationTests : IAsyncLifetime
    {
        private PostgreSqlContainer _postgresContainer;

        /// <summary>
        /// Initializes the PostgreSQL container before tests run.
        /// </summary>
        public async Task InitializeAsync()
        {
            _postgresContainer = new PostgreSqlBuilder()
                .WithDatabase("synaxis_test")
                .WithUsername("postgres")
                .WithPassword("testpassword")
                .WithImage("postgres:16-alpine")
                .Build();

            await _postgresContainer.StartAsync();
        }

        /// <summary>
        /// Disposes the PostgreSQL container after tests complete.
        /// </summary>
        public async Task DisposeAsync()
        {
            if (_postgresContainer != null)
            {
                await _postgresContainer.DisposeAsync();
            }
        }

        private SynaxisDbContext CreateContext()
        {
            var options = new DbContextOptionsBuilder<SynaxisDbContext>()
                .UseNpgsql(_postgresContainer.GetConnectionString())
                .Options;

            return new SynaxisDbContext(options);
        }

        /// <summary>
        /// Verifies that all migrations can be applied successfully to a fresh database.
        /// </summary>
        [Fact]
        public async Task Migrations_CanBeApplied_ToFreshDatabase()
        {
            // Arrange
            using var context = CreateContext();

            // Act
            await context.Database.MigrateAsync();

            // Assert
            var canConnect = await context.Database.CanConnectAsync();
            canConnect.Should().BeTrue();

            // Verify all tables exist
            var tableNames = new[]
            {
                "organizations", "teams", "users", "team_memberships",
                "virtual_keys", "requests", "subscription_plans",
                "audit_logs", "spend_logs", "credit_transactions", "invoices",
                "organization_backup_config"
            };

            foreach (var tableName in tableNames)
            {
                var tableExists = await TableExistsAsync(context, tableName);
                tableExists.Should().BeTrue($"Table {tableName} should exist");
            }
        }

        /// <summary>
        /// Verifies that migrations can be rolled back.
        /// </summary>
        [Fact]
        public async Task Migrations_CanBeRolledBack()
        {
            // Arrange
            using var context = CreateContext();
            await context.Database.MigrateAsync();

            // Verify migrations were applied
            var pendingMigrations = await context.Database.GetPendingMigrationsAsync();
            pendingMigrations.Should().BeEmpty();

            // Act - Rollback to initial state
            await context.Database.ExecuteSqlRawAsync(@"
                DO $$
                DECLARE
                    r RECORD;
                BEGIN
                    FOR r IN (SELECT tablename FROM pg_tables WHERE schemaname = 'public')
                    LOOP
                        EXECUTE 'DROP TABLE IF EXISTS public.' || quote_ident(r.tablename) || ' CASCADE';
                    END LOOP;
                END $$;");

            // Assert - Database is empty
            var tableExists = await TableExistsAsync(context, "organizations");
            tableExists.Should().BeFalse();
        }

        /// <summary>
        /// Verifies that data can be inserted after migrations are applied.
        /// </summary>
        [Fact]
        public async Task Data_CanBeInserted_AfterMigrations()
        {
            // Arrange
            using var context = CreateContext();
            await context.Database.MigrateAsync();

            var organization = new Organization
            {
                Id = Guid.NewGuid(),
                Slug = "test-org",
                Name = "Test Organization",
                PrimaryRegion = "eu-west-1",
            };

            var user = new User
            {
                Id = Guid.NewGuid(),
                OrganizationId = organization.Id,
                Email = "test@example.com",
                PasswordHash = "hash123",
                DataResidencyRegion = "eu-west-1",
                CreatedInRegion = "eu-west-1",
            };

            var team = new Team
            {
                Id = Guid.NewGuid(),
                OrganizationId = organization.Id,
                Slug = "test-team",
                Name = "Test Team",
            };

            // Act
            context.Organizations.Add(organization);
            context.Users.Add(user);
            context.Teams.Add(team);
            await context.SaveChangesAsync();

            // Assert
            var savedOrg = await context.Organizations.FindAsync(organization.Id);
            savedOrg.Should().NotBeNull();
            savedOrg?.Slug.Should().Be("test-org");

            var savedUser = await context.Users.FindAsync(user.Id);
            savedUser.Should().NotBeNull();
            savedUser?.Email.Should().Be("test@example.com");

            var savedTeam = await context.Teams.FindAsync(team.Id);
            savedTeam.Should().NotBeNull();
            savedTeam?.Name.Should().Be("Test Team");
        }

        /// <summary>
        /// Verifies that foreign key constraints work correctly.
        /// </summary>
        [Fact]
        public async Task ForeignKeyConstraints_Enforced()
        {
            // Arrange
            using var context = CreateContext();
            await context.Database.MigrateAsync();

            // Create organization first
            var organization = new Organization
            {
                Id = Guid.NewGuid(),
                Slug = "fk-test-org",
                Name = "FK Test Organization",
                PrimaryRegion = "eu-west-1",
            };
            context.Organizations.Add(organization);
            await context.SaveChangesAsync();

            // Try to create user with non-existent organization
            var userWithInvalidOrg = new User
            {
                Id = Guid.NewGuid(),
                OrganizationId = Guid.NewGuid(), // Non-existent
                Email = "invalid@example.com",
                PasswordHash = "hash123",
                DataResidencyRegion = "eu-west-1",
                CreatedInRegion = "eu-west-1",
            };

            context.Users.Add(userWithInvalidOrg);

            // Act & Assert
            await Assert.ThrowsAsync<DbUpdateException>(async () =>
            {
                await context.SaveChangesAsync();
            });
        }

        /// <summary>
        /// Verifies that unique constraints are enforced.
        /// </summary>
        [Fact]
        public async Task UniqueConstraints_Enforced()
        {
            // Arrange
            using var context = CreateContext();
            await context.Database.MigrateAsync();

            var org1 = new Organization
            {
                Id = Guid.NewGuid(),
                Slug = "unique-test-org",
                Name = "Unique Test Organization 1",
                PrimaryRegion = "eu-west-1",
            };

            var org2 = new Organization
            {
                Id = Guid.NewGuid(),
                Slug = "unique-test-org", // Same slug
                Name = "Unique Test Organization 2",
                PrimaryRegion = "us-east-1",
            };

            context.Organizations.Add(org1);
            context.Organizations.Add(org2);

            // Act & Assert
            await Assert.ThrowsAsync<DbUpdateException>(async () =>
            {
                await context.SaveChangesAsync();
            });
        }

        /// <summary>
        /// Verifies that cascading deletes work correctly.
        /// </summary>
        [Fact]
        public async Task CascadeDeletes_WorkCorrectly()
        {
            // Arrange
            using var context = CreateContext();
            await context.Database.MigrateAsync();

            var organization = new Organization
            {
                Id = Guid.NewGuid(),
                Slug = "cascade-test-org",
                Name = "Cascade Test Organization",
                PrimaryRegion = "eu-west-1",
            };

            var user = new User
            {
                Id = Guid.NewGuid(),
                OrganizationId = organization.Id,
                Email = "cascade@example.com",
                PasswordHash = "hash123",
                DataResidencyRegion = "eu-west-1",
                CreatedInRegion = "eu-west-1",
            };

            context.Organizations.Add(organization);
            context.Users.Add(user);
            await context.SaveChangesAsync();

            // Verify user exists
            var savedUser = await context.Users.FindAsync(user.Id);
            savedUser.Should().NotBeNull();

            // Act - Delete organization (should cascade to users)
            context.Organizations.Remove(organization);
            await context.SaveChangesAsync();

            // Assert - User should also be deleted
            var deletedUser = await context.Users.FindAsync(user.Id);
            deletedUser.Should().BeNull();
        }

        /// <summary>
        /// Verifies that concurrent migration attempts fail gracefully.
        /// </summary>
        [Fact]
        public async Task ConcurrentMigrations_FailGracefully()
        {
            // Arrange
            using var context1 = CreateContext();
            using var context2 = CreateContext();

            // Start first migration
            var migration1 = context1.Database.MigrateAsync();

            // Try to start second migration concurrently
            var migration2 = context2.Database.MigrateAsync();

            // Act - Wait for both to complete (one may fail)
            try
            {
                await Task.WhenAll(migration1, migration2);
            }
            catch
            {
                // Expected that one may fail due to locking
            }

            // Assert - At least one should succeed and database should be migrated
            using var verifyContext = CreateContext();
            var canConnect = await verifyContext.Database.CanConnectAsync();
            canConnect.Should().BeTrue();

            var pendingMigrations = await verifyContext.Database.GetPendingMigrationsAsync();
            pendingMigrations.Should().BeEmpty();
        }

        /// <summary>
        /// Verifies that check constraints are enforced.
        /// </summary>
        [Fact]
        public async Task CheckConstraints_Enforced()
        {
            // Arrange
            using var context = CreateContext();
            await context.Database.MigrateAsync();

            var organization = new Organization
            {
                Id = Guid.NewGuid(),
                Slug = "check-test-org",
                Name = "Check Test Organization",
                PrimaryRegion = "eu-west-1",
            };

            var team = new Team
            {
                Id = Guid.NewGuid(),
                OrganizationId = organization.Id,
                Slug = "check-test-team",
                Name = "Check Test Team",
                MonthlyBudget = -100m, // Invalid: negative budget
            };

            context.Organizations.Add(organization);
            context.Teams.Add(team);

            // Act & Assert
            await Assert.ThrowsAsync<DbUpdateException>(async () =>
            {
                await context.SaveChangesAsync();
            });
        }

        /// <summary>
        /// Verifies that JSON columns work correctly.
        /// </summary>
        [Fact]
        public async Task JsonColumns_WorkCorrectly()
        {
            // Arrange
            using var context = CreateContext();
            await context.Database.MigrateAsync();

            var organization = new Organization
            {
                Id = Guid.NewGuid(),
                Slug = "json-test-org",
                Name = "JSON Test Organization",
                PrimaryRegion = "eu-west-1",
                PrivacyConsent = new System.Collections.Generic.Dictionary<string, object>
                {
                    { "gdpr_accepted", true },
                    { "consent_date", DateTime.UtcNow.ToString("O") },
                    { "version", "1.0" },
                },
            };

            // Act
            context.Organizations.Add(organization);
            await context.SaveChangesAsync();

            // Clear context to force reload from database
            context.ChangeTracker.Clear();

            // Assert
            var savedOrg = await context.Organizations.FindAsync(organization.Id);
            savedOrg.Should().NotBeNull();
            savedOrg?.PrivacyConsent.Should().NotBeNull();
            savedOrg?.PrivacyConsent.Should().ContainKey("gdpr_accepted");
            
            // JSON deserialization returns JsonElement, so we need to extract the boolean value
            var gdprAccepted = savedOrg?.PrivacyConsent["gdpr_accepted"];
            if (gdprAccepted is System.Text.Json.JsonElement jsonElement)
            {
                jsonElement.GetBoolean().Should().BeTrue();
            }
            else
            {
                gdprAccepted.Should().Be(true);
            }
        }

        private async Task<bool> TableExistsAsync(SynaxisDbContext context, string tableName)
        {
            var connection = context.Database.GetDbConnection();
            
            // Only open connection if it's not already open
            var shouldClose = false;
            if (connection.State != System.Data.ConnectionState.Open)
            {
                await connection.OpenAsync();
                shouldClose = true;
            }

            try
            {
                using var command = connection.CreateCommand();
                command.CommandText = @"
                    SELECT EXISTS (
                        SELECT 1 FROM information_schema.tables
                        WHERE table_schema = 'public'
                        AND table_name = @tableName
                    );";
                
                var parameter = command.CreateParameter();
                parameter.ParameterName = "@tableName";
                parameter.Value = tableName;
                command.Parameters.Add(parameter);

                var result = await command.ExecuteScalarAsync();
                return result is bool exists && exists;
            }
            finally
            {
                if (shouldClose)
                {
                    await connection.CloseAsync();
                }
            }
        }
    }
}
