// <copyright file="OrganizationsControllerTests.cs" company="Synaxis">
// Copyright (c) Synaxis. All rights reserved.
// </copyright>

namespace Synaxis.InferenceGateway.WebApi.Tests.Controllers
{
    using System;
    using System.Collections.Generic;
    using System.Globalization;
    using System.Linq;
    using System.Security.Claims;
    using System.Threading;
    using System.Threading.Tasks;
    using FluentAssertions;
    using Microsoft.AspNetCore.Http;
    using Microsoft.AspNetCore.Mvc;
    using Microsoft.EntityFrameworkCore;
    using Moq;
    using Synaxis.Core.Models;
    using Synaxis.Infrastructure.Data;
    using Synaxis.InferenceGateway.WebApi.Controllers;
    using Xunit;
    using OrganizationSettingsResponse = Synaxis.Core.Contracts.OrganizationSettingsResponse;

    [Trait("Category", "Unit")]
    public class OrganizationsControllerTests
    {
        private readonly Mock<SynaxisDbContext> _mockDbContext;
        private readonly OrganizationsController _controller;
        private readonly Guid _testUserId = Guid.NewGuid();
        private readonly Guid _testOrganizationId = Guid.NewGuid();

        public OrganizationsControllerTests()
        {
            var options = new DbContextOptionsBuilder<SynaxisDbContext>()
                .UseInMemoryDatabase(databaseName: $"TestDb_{Guid.NewGuid()}")
                .Options;
            _mockDbContext = new Mock<SynaxisDbContext>(options);

            _controller = new OrganizationsController(_mockDbContext.Object);
            SetupControllerContext();
        }

        [Fact]
        public async Task GetOrganizationSettings_WhenOrganizationDoesNotExist_ReturnsNotFound()
        {
            // Arrange
            var nonExistentOrgId = Guid.NewGuid();
            var mockSet = CreateMockDbSet(Array.Empty<Organization>());
            _mockDbContext.Setup(x => x.Organizations).Returns(mockSet.Object);

            // Act
            var result = await _controller.GetOrganizationSettings(nonExistentOrgId, CancellationToken.None);

            // Assert
            result.Should().BeOfType<NotFoundResult>();
        }

        [Fact]
        public async Task GetOrganizationSettings_WhenUserIsNotMember_ReturnsForbid()
        {
            // Arrange
            var organization = CreateTestOrganization();
            var mockOrgSet = CreateMockDbSet(new[] { organization });
            _mockDbContext.Setup(x => x.Organizations).Returns(mockOrgSet.Object);

            var mockMembershipSet = CreateMockDbSet(Array.Empty<TeamMembership>());
            _mockDbContext.Setup(x => x.TeamMemberships).Returns(mockMembershipSet.Object);

            // Act
            var result = await _controller.GetOrganizationSettings(_testOrganizationId, CancellationToken.None);

            // Assert
            result.Should().BeOfType<ForbidResult>();
        }

        [Fact]
        public async Task GetOrganizationSettings_WhenUserIsMember_ReturnsOkWithSettings()
        {
            // Arrange
            var organization = CreateTestOrganization();
            var mockOrgSet = CreateMockDbSet(new[] { organization });
            _mockDbContext.Setup(x => x.Organizations).Returns(mockOrgSet.Object);

            var membership = CreateTestTeamMembership(_testUserId, Guid.NewGuid(), _testOrganizationId);
            var mockMembershipSet = CreateMockDbSet(new[] { membership });
            _mockDbContext.Setup(x => x.TeamMemberships).Returns(mockMembershipSet.Object);

            // Act
            var result = await _controller.GetOrganizationSettings(_testOrganizationId, CancellationToken.None);

            // Assert
            result.Should().BeOfType<OkObjectResult>();
            var okResult = result as OkObjectResult;
            okResult!.Value.Should().BeOfType<OrganizationSettingsResponse>();

            var response = okResult.Value as OrganizationSettingsResponse;
            response!.Tier.Should().Be(organization.Tier);
            response.DataRetentionDays.Should().Be(organization.DataRetentionDays);
            response.RequireSso.Should().Be(organization.RequireSso);
            response.AllowedEmailDomains.Should().NotBeNull();
            response.AvailableRegions.Should().NotBeNull();
            response.PrivacyConsent.Should().NotBeNull();
        }

        [Fact]
        public async Task GetOrganizationSettings_WithCompleteSettings_ReturnsAllFields()
        {
            // Arrange
            var organization = CreateTestOrganization();
            organization.Tier = "enterprise";
            organization.DataRetentionDays = 90;
            organization.RequireSso = true;
            organization.AllowedEmailDomains = new List<string> { "example.com", "test.com" };
            organization.AvailableRegions = new List<string> { "us-east-1", "eu-west-1" };
            organization.PrivacyConsent = new Dictionary<string, object>
            {
                { "dataProcessing", true },
                { "marketing", false },
                { "analytics", true }
            };

            var mockOrgSet = CreateMockDbSet(new[] { organization });
            _mockDbContext.Setup(x => x.Organizations).Returns(mockOrgSet.Object);

            var membership = CreateTestTeamMembership(_testUserId, Guid.NewGuid(), _testOrganizationId);
            var mockMembershipSet = CreateMockDbSet(new[] { membership });
            _mockDbContext.Setup(x => x.TeamMemberships).Returns(mockMembershipSet.Object);

            // Act
            var result = await _controller.GetOrganizationSettings(_testOrganizationId, CancellationToken.None);

            // Assert
            result.Should().BeOfType<OkObjectResult>();
            var okResult = result as OkObjectResult;
            var response = okResult!.Value as OrganizationSettingsResponse;

            response!.Tier.Should().Be("enterprise");
            response.DataRetentionDays.Should().Be(90);
            response.RequireSso.Should().BeTrue();
            response.AllowedEmailDomains.Should().HaveCount(2);
            response.AllowedEmailDomains.Should().Contain("example.com");
            response.AllowedEmailDomains.Should().Contain("test.com");
            response.AvailableRegions.Should().HaveCount(2);
            response.AvailableRegions.Should().Contain("us-east-1");
            response.AvailableRegions.Should().Contain("eu-west-1");
            response.PrivacyConsent.Should().HaveCount(3);
            response.PrivacyConsent["dataProcessing"].Should().Be(true);
            response.PrivacyConsent["marketing"].Should().Be(false);
            response.PrivacyConsent["analytics"].Should().Be(true);
        }

        [Fact]
        public async Task GetOrganizationSettings_WithMinimalSettings_ReturnsDefaultValues()
        {
            // Arrange
            var organization = CreateTestOrganization();
            organization.AllowedEmailDomains = null;
            organization.AvailableRegions = null;
            organization.PrivacyConsent = null;

            var mockOrgSet = CreateMockDbSet(new[] { organization });
            _mockDbContext.Setup(x => x.Organizations).Returns(mockOrgSet.Object);

            var membership = CreateTestTeamMembership(_testUserId, Guid.NewGuid(), _testOrganizationId);
            var mockMembershipSet = CreateMockDbSet(new[] { membership });
            _mockDbContext.Setup(x => x.TeamMemberships).Returns(mockMembershipSet.Object);

            // Act
            var result = await _controller.GetOrganizationSettings(_testOrganizationId, CancellationToken.None);

            // Assert
            result.Should().BeOfType<OkObjectResult>();
            var okResult = result as OkObjectResult;
            var response = okResult!.Value as OrganizationSettingsResponse;

            response!.Tier.Should().Be("free");
            response.DataRetentionDays.Should().Be(30);
            response.RequireSso.Should().BeFalse();
        }

        [Fact]
        public async Task GetOrganizationSettings_WithMultipleMemberships_AllowsAccess()
        {
            // Arrange
            var organization = CreateTestOrganization();
            var mockOrgSet = CreateMockDbSet(new[] { organization });
            _mockDbContext.Setup(x => x.Organizations).Returns(mockOrgSet.Object);

            var teamId1 = Guid.NewGuid();
            var teamId2 = Guid.NewGuid();
            var membership1 = CreateTestTeamMembership(_testUserId, teamId1, _testOrganizationId);
            var membership2 = CreateTestTeamMembership(_testUserId, teamId2, _testOrganizationId);
            var mockMembershipSet = CreateMockDbSet(new[] { membership1, membership2 });
            _mockDbContext.Setup(x => x.TeamMemberships).Returns(mockMembershipSet.Object);

            // Act
            var result = await _controller.GetOrganizationSettings(_testOrganizationId, CancellationToken.None);

            // Assert
            result.Should().BeOfType<OkObjectResult>();
        }

        [Fact]
        public async Task GetOrganizationSettings_WithDifferentUserAndOrganization_ReturnsForbid()
        {
            // Arrange
            var differentOrgId = Guid.NewGuid();
            var organization = CreateTestOrganization();
            organization.Id = differentOrgId;
            var mockOrgSet = CreateMockDbSet(new[] { organization });
            _mockDbContext.Setup(x => x.Organizations).Returns(mockOrgSet.Object);

            // Membership is for a different organization
            var membership = CreateTestTeamMembership(_testUserId, Guid.NewGuid(), _testOrganizationId);
            var mockMembershipSet = CreateMockDbSet(new[] { membership });
            _mockDbContext.Setup(x => x.TeamMemberships).Returns(mockMembershipSet.Object);

            // Act
            var result = await _controller.GetOrganizationSettings(differentOrgId, CancellationToken.None);

            // Assert
            result.Should().BeOfType<ForbidResult>();
        }

        private Organization CreateTestOrganization()
        {
            return new Organization
            {
                Id = _testOrganizationId,
                Name = "Test Organization",
                Slug = "test-org",
                Description = "Test organization",
                PrimaryRegion = "us-east-1",
                Tier = "free",
                BillingCurrency = "USD",
                CreditBalance = 0.00m,
                CreditCurrency = "USD",
                SubscriptionStatus = "active",
                IsTrial = false,
                DataRetentionDays = 30,
                RequireSso = false,
                IsActive = true,
                IsVerified = false,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
                AllowedEmailDomains = new List<string>(),
                AvailableRegions = new List<string>(),
                PrivacyConsent = new Dictionary<string, object>()
            };
        }

        private TeamMembership CreateTestTeamMembership(Guid userId, Guid teamId, Guid organizationId)
        {
            return new TeamMembership
            {
                Id = Guid.NewGuid(),
                UserId = userId,
                TeamId = teamId,
                OrganizationId = organizationId,
                Role = "Member",
                JoinedAt = DateTime.UtcNow
            };
        }

        private static Mock<DbSet<T>> CreateMockDbSet<T>(IEnumerable<T> data)
            where T : class
        {
            var queryableData = data.AsQueryable();
            var mockSet = new Mock<DbSet<T>>();

            var testAsyncProvider = new TestAsyncQueryProvider<T>(queryableData.Provider);
            mockSet.As<IQueryable<T>>().Setup(m => m.Provider).Returns(testAsyncProvider);
            mockSet.As<IQueryable<T>>().Setup(m => m.Expression).Returns(queryableData.Expression);
            mockSet.As<IQueryable<T>>().Setup(m => m.ElementType).Returns(queryableData.ElementType);
            mockSet.As<IQueryable<T>>().Setup(m => m.GetEnumerator()).Returns(() => queryableData.GetEnumerator());
            mockSet.As<IAsyncEnumerable<T>>().Setup(m => m.GetAsyncEnumerator(It.IsAny<CancellationToken>())).Returns(() => new TestAsyncEnumerator<T>(queryableData.GetEnumerator()));

            return mockSet;
        }

        private void SetupControllerContext()
        {
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, _testUserId.ToString()),
                new Claim("sub", _testUserId.ToString())
            };

            var identity = new ClaimsIdentity(claims, "TestAuth");
            var claimsPrincipal = new ClaimsPrincipal(identity);

            _controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext
                {
                    User = claimsPrincipal
                }
            };
        }
    }
}
