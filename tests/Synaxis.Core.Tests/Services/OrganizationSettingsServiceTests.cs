// <copyright file="OrganizationSettingsServiceTests.cs" company="Synaxis">
// Copyright (c) Synaxis. All rights reserved.
// </copyright>

namespace Synaxis.Core.Tests.Services
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading;
    using System.Threading.Tasks;
    using FluentAssertions;
    using Microsoft.EntityFrameworkCore;
    using Synaxis.Core.Contracts;
    using Synaxis.Core.Models;
    using Synaxis.Infrastructure.Data;
    using Synaxis.Infrastructure.Services;
    using Xunit;

    /// <summary>
    /// Unit tests for OrganizationSettingsService.
    /// </summary>
    [Trait("Category", "Unit")]
    public class OrganizationSettingsServiceTests : IDisposable
    {
        private readonly SynaxisDbContext _context;
        private readonly OrganizationSettingsService _service;

        /// <summary>
        /// Initializes a new instance of the <see cref="OrganizationSettingsServiceTests"/> class.
        /// </summary>
        public OrganizationSettingsServiceTests()
        {
            var options = new DbContextOptionsBuilder<SynaxisDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;

            _context = new SynaxisDbContext(options);
            _service = new OrganizationSettingsService(_context);
        }

        /// <summary>
        /// Disposes the test context.
        /// </summary>
        public void Dispose()
        {
            _context.Dispose();
        }

        [Fact]
        public async Task GetOrganizationLimitsAsync_OrganizationExists_ReturnsLimits()
        {
            // Arrange
            var orgId = Guid.NewGuid();
            var organization = new Organization
            {
                Id = orgId,
                Slug = "test-org",
                Name = "Test Org",
                PrimaryRegion = "us-east-1",
                MaxTeams = 10,
                MaxUsersPerTeam = 50,
                MaxKeysPerUser = 5,
                MaxConcurrentRequests = 100,
                MonthlyRequestLimit = 1000000,
                MonthlyTokenLimit = 5000000,
            };

            _context.Organizations.Add(organization);
            await _context.SaveChangesAsync();

            // Act
            var result = await _service.GetOrganizationLimitsAsync(orgId, CancellationToken.None);

            // Assert
            result.Should().NotBeNull();
            result.MaxTeams.Should().Be(10);
            result.MaxUsersPerTeam.Should().Be(50);
            result.MaxKeysPerUser.Should().Be(5);
            result.MaxConcurrentRequests.Should().Be(100);
            result.MonthlyRequestLimit.Should().Be(1000000);
            result.MonthlyTokenLimit.Should().Be(5000000);
        }

        [Fact]
        public async Task GetOrganizationLimitsAsync_OrganizationNotFound_ThrowsKeyNotFoundException()
        {
            // Arrange
            var orgId = Guid.NewGuid();

            // Act
            Func<Task> act = async () => await _service.GetOrganizationLimitsAsync(orgId, CancellationToken.None);

            // Assert
            await act.Should().ThrowAsync<KeyNotFoundException>()
                .WithMessage($"Organization with ID {orgId} not found.");
        }

        [Fact]
        public async Task GetOrganizationLimitsAsync_NullLimits_ReturnsNull()
        {
            // Arrange
            var orgId = Guid.NewGuid();
            var organization = new Organization
            {
                Id = orgId,
                Slug = "test-org",
                Name = "Test Org",
                PrimaryRegion = "us-east-1",
                MaxTeams = null,
                MaxUsersPerTeam = null,
                MaxKeysPerUser = null,
                MaxConcurrentRequests = null,
                MonthlyRequestLimit = null,
                MonthlyTokenLimit = null,
            };

            _context.Organizations.Add(organization);
            await _context.SaveChangesAsync();

            // Act
            var result = await _service.GetOrganizationLimitsAsync(orgId, CancellationToken.None);

            // Assert
            result.Should().NotBeNull();
            result.MaxTeams.Should().BeNull();
            result.MaxUsersPerTeam.Should().BeNull();
            result.MaxKeysPerUser.Should().BeNull();
            result.MaxConcurrentRequests.Should().BeNull();
            result.MonthlyRequestLimit.Should().BeNull();
            result.MonthlyTokenLimit.Should().BeNull();
        }

        [Fact]
        public async Task UpdateOrganizationLimitsAsync_ValidRequest_UpdatesLimits()
        {
            // Arrange
            var orgId = Guid.NewGuid();
            var userId = Guid.NewGuid();
            var organization = new Organization
            {
                Id = orgId,
                Slug = "test-org",
                Name = "Test Org",
                PrimaryRegion = "us-east-1",
                MaxTeams = 5,
                MaxUsersPerTeam = 25,
            };

            _context.Organizations.Add(organization);
            await _context.SaveChangesAsync();

            var request = new UpdateOrganizationLimitsRequest
            {
                MaxTeams = 20,
                MaxUsersPerTeam = 100,
                MaxKeysPerUser = 10,
            };

            // Act
            var result = await _service.UpdateOrganizationLimitsAsync(orgId, request, userId, CancellationToken.None);

            // Assert
            result.Should().NotBeNull();
            result.MaxTeams.Should().Be(20);
            result.MaxUsersPerTeam.Should().Be(100);
            result.MaxKeysPerUser.Should().Be(10);

            // Verify in database
            var updatedOrg = await _context.Organizations.FindAsync(orgId);
            updatedOrg.Should().NotBeNull();
            updatedOrg!.MaxTeams.Should().Be(20);
            updatedOrg.MaxUsersPerTeam.Should().Be(100);
            updatedOrg.MaxKeysPerUser.Should().Be(10);
        }

        [Fact]
        public async Task UpdateOrganizationLimitsAsync_OrganizationNotFound_ThrowsKeyNotFoundException()
        {
            // Arrange
            var orgId = Guid.NewGuid();
            var userId = Guid.NewGuid();
            var request = new UpdateOrganizationLimitsRequest
            {
                MaxTeams = 20,
            };

            // Act
            Func<Task> act = async () => await _service.UpdateOrganizationLimitsAsync(orgId, request, userId, CancellationToken.None);

            // Assert
            await act.Should().ThrowAsync<KeyNotFoundException>()
                .WithMessage($"Organization with ID {orgId} not found.");
        }

        [Fact]
        public async Task UpdateOrganizationLimitsAsync_NegativeMaxTeams_ThrowsArgumentException()
        {
            // Arrange
            var orgId = Guid.NewGuid();
            var userId = Guid.NewGuid();
            var organization = new Organization
            {
                Id = orgId,
                Slug = "test-org",
                Name = "Test Org",
                PrimaryRegion = "us-east-1",
            };

            _context.Organizations.Add(organization);
            await _context.SaveChangesAsync();

            var request = new UpdateOrganizationLimitsRequest
            {
                MaxTeams = -5,
            };

            // Act
            Func<Task> act = async () => await _service.UpdateOrganizationLimitsAsync(orgId, request, userId, CancellationToken.None);

            // Assert
            await act.Should().ThrowAsync<ArgumentException>()
                .WithMessage("*MaxTeams*");
        }

        [Fact]
        public async Task UpdateOrganizationLimitsAsync_NegativeMonthlyRequestLimit_ThrowsArgumentException()
        {
            // Arrange
            var orgId = Guid.NewGuid();
            var userId = Guid.NewGuid();
            var organization = new Organization
            {
                Id = orgId,
                Slug = "test-org",
                Name = "Test Org",
                PrimaryRegion = "us-east-1",
            };

            _context.Organizations.Add(organization);
            await _context.SaveChangesAsync();

            var request = new UpdateOrganizationLimitsRequest
            {
                MonthlyRequestLimit = -1000,
            };

            // Act
            Func<Task> act = async () => await _service.UpdateOrganizationLimitsAsync(orgId, request, userId, CancellationToken.None);

            // Assert
            await act.Should().ThrowAsync<ArgumentException>()
                .WithMessage("*MonthlyRequestLimit*");
        }

        [Fact]
        public async Task GetOrganizationSettingsAsync_OrganizationExists_ReturnsSettings()
        {
            // Arrange
            var orgId = Guid.NewGuid();
            var organization = new Organization
            {
                Id = orgId,
                Slug = "test-org",
                Name = "Test Org",
                PrimaryRegion = "us-east-1",
                Tier = "pro",
                DataRetentionDays = 90,
                RequireSso = true,
                AllowedEmailDomains = new List<string> { "example.com", "test.com" },
                AvailableRegions = new List<string> { "us-east-1", "eu-west-1" },
                PrivacyConsent = new Dictionary<string, object>
                {
                    { "gdpr", true },
                    { "acceptedAt", DateTime.UtcNow.ToString("O") },
                },
            };

            _context.Organizations.Add(organization);
            await _context.SaveChangesAsync();

            // Act
            var result = await _service.GetOrganizationSettingsAsync(orgId, CancellationToken.None);

            // Assert
            result.Should().NotBeNull();
            result.Tier.Should().Be("pro");
            result.DataRetentionDays.Should().Be(90);
            result.RequireSso.Should().BeTrue();
            result.AllowedEmailDomains.Should().BeEquivalentTo(new[] { "example.com", "test.com" });
            result.AvailableRegions.Should().BeEquivalentTo(new[] { "us-east-1", "eu-west-1" });
            result.PrivacyConsent.Should().ContainKey("gdpr");
        }

        [Fact]
        public async Task GetOrganizationSettingsAsync_OrganizationNotFound_ThrowsKeyNotFoundException()
        {
            // Arrange
            var orgId = Guid.NewGuid();

            // Act
            Func<Task> act = async () => await _service.GetOrganizationSettingsAsync(orgId, CancellationToken.None);

            // Assert
            await act.Should().ThrowAsync<KeyNotFoundException>()
                .WithMessage($"Organization with ID {orgId} not found.");
        }

        [Fact]
        public async Task UpdateOrganizationSettingsAsync_ValidRequest_UpdatesSettings()
        {
            // Arrange
            var orgId = Guid.NewGuid();
            var userId = Guid.NewGuid();
            var organization = new Organization
            {
                Id = orgId,
                Slug = "test-org",
                Name = "Test Org",
                PrimaryRegion = "us-east-1",
                Tier = "free",
                DataRetentionDays = 30,
                RequireSso = false,
                AllowedEmailDomains = new List<string>(),
            };

            _context.Organizations.Add(organization);
            await _context.SaveChangesAsync();

            var request = new UpdateOrganizationSettingsRequest
            {
                DataRetentionDays = 180,
                RequireSso = true,
                AllowedEmailDomains = new List<string> { "company.com" },
            };

            // Act
            var result = await _service.UpdateOrganizationSettingsAsync(orgId, request, userId, CancellationToken.None);

            // Assert
            result.Should().NotBeNull();
            result.DataRetentionDays.Should().Be(180);
            result.RequireSso.Should().BeTrue();
            result.AllowedEmailDomains.Should().BeEquivalentTo(new[] { "company.com" });

            // Verify in database
            var updatedOrg = await _context.Organizations.FindAsync(orgId);
            updatedOrg.Should().NotBeNull();
            updatedOrg!.DataRetentionDays.Should().Be(180);
            updatedOrg.RequireSso.Should().BeTrue();
            updatedOrg.AllowedEmailDomains.Should().BeEquivalentTo(new[] { "company.com" });
        }

        [Fact]
        public async Task UpdateOrganizationSettingsAsync_OrganizationNotFound_ThrowsKeyNotFoundException()
        {
            // Arrange
            var orgId = Guid.NewGuid();
            var userId = Guid.NewGuid();
            var request = new UpdateOrganizationSettingsRequest
            {
                DataRetentionDays = 180,
                AllowedEmailDomains = new List<string>(),
            };

            // Act
            Func<Task> act = async () => await _service.UpdateOrganizationSettingsAsync(orgId, request, userId, CancellationToken.None);

            // Assert
            await act.Should().ThrowAsync<KeyNotFoundException>()
                .WithMessage($"Organization with ID {orgId} not found.");
        }

        [Fact]
        public async Task UpdateOrganizationSettingsAsync_InvalidDataRetentionDays_ThrowsArgumentException()
        {
            // Arrange
            var orgId = Guid.NewGuid();
            var userId = Guid.NewGuid();
            var organization = new Organization
            {
                Id = orgId,
                Slug = "test-org",
                Name = "Test Org",
                PrimaryRegion = "us-east-1",
                Tier = "free",
            };

            _context.Organizations.Add(organization);
            await _context.SaveChangesAsync();

            var request = new UpdateOrganizationSettingsRequest
            {
                DataRetentionDays = -5,
                AllowedEmailDomains = new List<string>(),
            };

            // Act
            Func<Task> act = async () => await _service.UpdateOrganizationSettingsAsync(orgId, request, userId, CancellationToken.None);

            // Assert
            await act.Should().ThrowAsync<ArgumentException>()
                .WithMessage("*DataRetentionDays*");
        }

        [Fact]
        public async Task UpdateOrganizationSettingsAsync_ExcessiveDataRetentionDays_ThrowsArgumentException()
        {
            // Arrange
            var orgId = Guid.NewGuid();
            var userId = Guid.NewGuid();
            var organization = new Organization
            {
                Id = orgId,
                Slug = "test-org",
                Name = "Test Org",
                PrimaryRegion = "us-east-1",
                Tier = "free",
            };

            _context.Organizations.Add(organization);
            await _context.SaveChangesAsync();

            var request = new UpdateOrganizationSettingsRequest
            {
                DataRetentionDays = 10000,
                AllowedEmailDomains = new List<string>(),
            };

            // Act
            Func<Task> act = async () => await _service.UpdateOrganizationSettingsAsync(orgId, request, userId, CancellationToken.None);

            // Assert
            await act.Should().ThrowAsync<ArgumentException>()
                .WithMessage("*DataRetentionDays*");
        }
    }
}
