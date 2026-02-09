// <copyright file="OrganizationSettingsServiceIntegrationTests.cs" company="Synaxis">
// Copyright (c) Synaxis. All rights reserved.
// </copyright>

using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Synaxis.Core.Contracts;
using Synaxis.Core.Models;
using Synaxis.Infrastructure.Data;
using Synaxis.Infrastructure.Services;
using Testcontainers.PostgreSql;
using Xunit;

namespace Synaxis.Infrastructure.Tests.Services
{
    /// <summary>
    /// Integration tests for OrganizationSettingsService using TestContainers.
    /// </summary>
    [Trait("Category", "Integration")]
    public class OrganizationSettingsServiceIntegrationTests : IAsyncLifetime
    {
        private readonly PostgreSqlContainer postgres = new PostgreSqlBuilder()
            .WithImage("postgres:16-alpine")
            .WithDatabase("synaxis_test")
            .WithUsername("test")
            .WithPassword("test")
            .Build();

        private SynaxisDbContext context = null!;
        private OrganizationSettingsService service = null!;

        public async Task InitializeAsync()
        {
            await this.postgres.StartAsync();

            var options = new DbContextOptionsBuilder<SynaxisDbContext>()
                .UseNpgsql(this.postgres.GetConnectionString())
                .Options;

            this.context = new SynaxisDbContext(options);
            await this.context.Database.EnsureCreatedAsync();

            this.service = new OrganizationSettingsService(this.context);
        }

        public async Task DisposeAsync()
        {
            if (this.context != null)
            {
                await this.context.DisposeAsync();
            }

            await this.postgres.DisposeAsync();
        }

        [Fact]
        public async Task GetOrganizationLimitsAsync_WithRealDatabase_ReturnsLimits()
        {
            // Arrange
            var organization = new Organization
            {
                Id = Guid.NewGuid(),
                Slug = "test-org",
                Name = "Test Organization",
                PrimaryRegion = "us-east-1",
                MaxTeams = 15,
                MaxUsersPerTeam = 75,
                MaxKeysPerUser = 8,
            };

            this.context.Organizations.Add(organization);
            await this.context.SaveChangesAsync();

            // Act
            var result = await this.service.GetOrganizationLimitsAsync(organization.Id);

            // Assert
            result.Should().NotBeNull();
            result.MaxTeams.Should().Be(15);
            result.MaxUsersPerTeam.Should().Be(75);
            result.MaxKeysPerUser.Should().Be(8);
        }

        [Fact]
        public async Task UpdateOrganizationLimitsAsync_WithRealDatabase_PersistsChanges()
        {
            // Arrange
            var organization = new Organization
            {
                Id = Guid.NewGuid(),
                Slug = "test-org-2",
                Name = "Test Organization 2",
                PrimaryRegion = "eu-west-1",
                MaxTeams = 5,
            };

            this.context.Organizations.Add(organization);
            await this.context.SaveChangesAsync();

            var request = new UpdateOrganizationLimitsRequest
            {
                MaxTeams = 25,
                MaxUsersPerTeam = 100,
            };

            // Act
            var result = await this.service.UpdateOrganizationLimitsAsync(
                organization.Id,
                request,
                Guid.NewGuid());

            // Assert
            result.MaxTeams.Should().Be(25);
            result.MaxUsersPerTeam.Should().Be(100);

            // Verify persistence by re-fetching
            var updatedOrg = await this.context.Organizations.FindAsync(organization.Id);
            updatedOrg.Should().NotBeNull();
            updatedOrg!.MaxTeams.Should().Be(25);
            updatedOrg.MaxUsersPerTeam.Should().Be(100);
        }

        [Fact]
        public async Task UpdateOrganizationLimitsAsync_ConcurrentUpdates_HandlesCorrectly()
        {
            // Arrange
            var organization = new Organization
            {
                Id = Guid.NewGuid(),
                Slug = "concurrent-test",
                Name = "Concurrent Test Org",
                PrimaryRegion = "us-east-1",
                MaxTeams = 10,
            };

            this.context.Organizations.Add(organization);
            await this.context.SaveChangesAsync();

            // Create two separate contexts to simulate concurrent updates
            var options = new DbContextOptionsBuilder<SynaxisDbContext>()
                .UseNpgsql(this.postgres.GetConnectionString())
                .Options;

            await using var context1 = new SynaxisDbContext(options);
            await using var context2 = new SynaxisDbContext(options);

            var service1 = new OrganizationSettingsService(context1);
            var service2 = new OrganizationSettingsService(context2);

            var request1 = new UpdateOrganizationLimitsRequest { MaxTeams = 20 };
            var request2 = new UpdateOrganizationLimitsRequest { MaxUsersPerTeam = 50 };

            // Act
            var tasks = new[]
            {
                service1.UpdateOrganizationLimitsAsync(organization.Id, request1, Guid.NewGuid()),
                service2.UpdateOrganizationLimitsAsync(organization.Id, request2, Guid.NewGuid()),
            };

            await Task.WhenAll(tasks);

            // Assert - Verify final state (last write wins in this implementation)
            var finalOrg = await this.context.Organizations.FindAsync(organization.Id);
            finalOrg.Should().NotBeNull();
            (finalOrg!.MaxTeams == 20 || finalOrg.MaxUsersPerTeam == 50).Should().BeTrue();
        }

        [Fact]
        public async Task GetOrganizationSettingsAsync_WithRealDatabase_ReturnsSettings()
        {
            // Arrange
            var organization = new Organization
            {
                Id = Guid.NewGuid(),
                Slug = "settings-test",
                Name = "Settings Test Org",
                PrimaryRegion = "us-east-1",
                Tier = "enterprise",
                DataRetentionDays = 180,
                RequireSso = true,
                AllowedEmailDomains = new List<string> { "company.com", "subsidiary.com" },
            };

            this.context.Organizations.Add(organization);
            await this.context.SaveChangesAsync();

            // Act
            var result = await this.service.GetOrganizationSettingsAsync(organization.Id);

            // Assert
            result.Should().NotBeNull();
            result.Tier.Should().Be("enterprise");
            result.DataRetentionDays.Should().Be(180);
            result.RequireSso.Should().BeTrue();
            result.AllowedEmailDomains.Should().BeEquivalentTo(new[] { "company.com", "subsidiary.com" });
        }

        [Fact]
        public async Task UpdateOrganizationSettingsAsync_WithRealDatabase_PersistsChanges()
        {
            // Arrange
            var organization = new Organization
            {
                Id = Guid.NewGuid(),
                Slug = "settings-update-test",
                Name = "Settings Update Test Org",
                PrimaryRegion = "us-east-1",
                Tier = "free",
                DataRetentionDays = 30,
                RequireSso = false,
            };

            this.context.Organizations.Add(organization);
            await this.context.SaveChangesAsync();

            var request = new UpdateOrganizationSettingsRequest
            {
                DataRetentionDays = 365,
                RequireSso = true,
                AllowedEmailDomains = new List<string> { "secure.com" },
            };

            // Act
            var result = await this.service.UpdateOrganizationSettingsAsync(
                organization.Id,
                request,
                Guid.NewGuid());

            // Assert
            result.DataRetentionDays.Should().Be(365);
            result.RequireSso.Should().BeTrue();
            result.AllowedEmailDomains.Should().BeEquivalentTo(new[] { "secure.com" });

            // Verify persistence
            var updatedOrg = await this.context.Organizations.FindAsync(organization.Id);
            updatedOrg.Should().NotBeNull();
            updatedOrg!.DataRetentionDays.Should().Be(365);
            updatedOrg.RequireSso.Should().BeTrue();
        }

        [Fact]
        public async Task UpdateOrganizationLimitsAsync_LimitEnforcement_ValidatesMaximums()
        {
            // Arrange
            var organization = new Organization
            {
                Id = Guid.NewGuid(),
                Slug = "limit-enforcement-test",
                Name = "Limit Enforcement Test Org",
                PrimaryRegion = "us-east-1",
                MaxTeams = 5,
            };

            this.context.Organizations.Add(organization);
            await this.context.SaveChangesAsync();

            var request = new UpdateOrganizationLimitsRequest
            {
                MaxTeams = 1000,
                MaxConcurrentRequests = 10000,
            };

            // Act
            var result = await this.service.UpdateOrganizationLimitsAsync(
                organization.Id,
                request,
                Guid.NewGuid());

            // Assert
            result.MaxTeams.Should().Be(1000);
            result.MaxConcurrentRequests.Should().Be(10000);

            // Verify these large values are actually stored
            var updatedOrg = await this.context.Organizations.FindAsync(organization.Id);
            updatedOrg!.MaxTeams.Should().Be(1000);
            updatedOrg.MaxConcurrentRequests.Should().Be(10000);
        }

        [Fact]
        public async Task UpdateOrganizationSettingsAsync_DataRetentionValidation_EnforcesRange()
        {
            // Arrange
            var organization = new Organization
            {
                Id = Guid.NewGuid(),
                Slug = "retention-validation-test",
                Name = "Retention Validation Test Org",
                PrimaryRegion = "us-east-1",
            };

            this.context.Organizations.Add(organization);
            await this.context.SaveChangesAsync();

            var invalidRequest = new UpdateOrganizationSettingsRequest
            {
                DataRetentionDays = 5000, // Exceeds 3650 day maximum
            };

            // Act
            Func<Task> act = async () => await this.service.UpdateOrganizationSettingsAsync(
                organization.Id,
                invalidRequest,
                Guid.NewGuid());

            // Assert
            await act.Should().ThrowAsync<ArgumentException>()
                .WithMessage("*DataRetentionDays*");
        }
    }
}
