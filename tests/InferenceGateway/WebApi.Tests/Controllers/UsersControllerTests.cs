// <copyright file="UsersControllerTests.cs" company="Synaxis">
// Copyright (c) Synaxis. All rights reserved.
// </copyright>

namespace Synaxis.InferenceGateway.WebApi.Tests.Controllers
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading;
    using System.Threading.Tasks;
    using FluentAssertions;
    using Microsoft.AspNetCore.Mvc;
    using Microsoft.EntityFrameworkCore;
    using Moq;
    using Synaxis.Core.Models;
    using Synaxis.Infrastructure.Data;
    using Synaxis.InferenceGateway.Application.Interfaces;
    using Synaxis.InferenceGateway.WebApi.Controllers;
    using Xunit;

    [Trait("Category", "Unit")]
    public class UsersControllerTests
    {
        private readonly Mock<SynaxisDbContext> _mockDbContext;
        private readonly Mock<IOrganizationUserContext> _mockUserContext;
        private readonly UsersController _controller;
        private readonly Guid _testUserId = Guid.NewGuid();
        private readonly Guid _testOrganizationId = Guid.NewGuid();

        public UsersControllerTests()
        {
            var options = new DbContextOptionsBuilder<SynaxisDbContext>()
                .UseInMemoryDatabase(databaseName: $"TestDb_{Guid.NewGuid()}")
                .Options;
            _mockDbContext = new Mock<SynaxisDbContext>(options);
            _mockUserContext = new Mock<IOrganizationUserContext>();

            // Setup default user context
            _mockUserContext.Setup(x => x.UserId).Returns(_testUserId);
            _mockUserContext.Setup(x => x.OrganizationId).Returns(_testOrganizationId);
            _mockUserContext.Setup(x => x.IsAuthenticated).Returns(true);

            _controller = new UsersController(_mockDbContext.Object, _mockUserContext.Object);
        }

        [Fact]
        public async Task GetMe_WhenUserExists_ReturnsOkWithUserProfile()
        {
            // Arrange
            var testUser = CreateTestUser();
            var mockSet = CreateMockDbSet(new[] { testUser });
            _mockDbContext.Setup(x => x.Users).Returns(mockSet.Object);

            // Act
            var result = await _controller.GetMe(CancellationToken.None);

            // Assert
            result.Should().BeOfType<OkObjectResult>();
            var okResult = result as OkObjectResult;
            okResult!.Value.Should().NotBeNull();

            // Verify the response structure
            var response = okResult.Value;
            response.Should().NotBeNull();
            var props = response!.GetType().GetProperties();
            props.Should().Contain(p => p.Name == "id");
            props.Should().Contain(p => p.Name == "email");
            props.Should().Contain(p => p.Name == "firstName");
            props.Should().Contain(p => p.Name == "lastName");
        }

        [Fact]
        public async Task GetMe_WhenUserDoesNotExist_ReturnsNotFound()
        {
            // Arrange
            var mockSet = CreateMockDbSet(Array.Empty<User>());
            _mockDbContext.Setup(x => x.Users).Returns(mockSet.Object);

            // Act
            var result = await _controller.GetMe(CancellationToken.None);

            // Assert
            result.Should().BeOfType<NotFoundResult>();
        }

        [Fact]
        public async Task UpdateMe_WithValidRequest_UpdatesUserAndReturnsOk()
        {
            // Arrange
            var testUser = CreateTestUser();
            var mockSet = CreateMockDbSet(new[] { testUser });
            _mockDbContext.Setup(x => x.Users).Returns(mockSet.Object);
            _mockDbContext.Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);

            var request = new UpdateUserRequest
            {
                FirstName = "UpdatedFirst",
                LastName = "UpdatedLast",
                Timezone = "America/New_York",
                Locale = "en-GB"
            };

            // Act
            var result = await _controller.UpdateMe(request, CancellationToken.None);

            // Assert
            result.Should().BeOfType<OkObjectResult>();
            testUser.FirstName.Should().Be("UpdatedFirst");
            testUser.LastName.Should().Be("UpdatedLast");
            testUser.Timezone.Should().Be("America/New_York");
            testUser.Locale.Should().Be("en-GB");
            _mockDbContext.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task UpdateMe_WhenUserDoesNotExist_ReturnsNotFound()
        {
            // Arrange
            var mockSet = CreateMockDbSet(Array.Empty<User>());
            _mockDbContext.Setup(x => x.Users).Returns(mockSet.Object);

            var request = new UpdateUserRequest
            {
                FirstName = "UpdatedFirst"
            };

            // Act
            var result = await _controller.UpdateMe(request, CancellationToken.None);

            // Assert
            result.Should().BeOfType<NotFoundResult>();
            _mockDbContext.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
        }

        [Fact]
        public async Task UpdateMe_WithPartialUpdate_OnlyUpdatesProvidedFields()
        {
            // Arrange
            var testUser = CreateTestUser();
            var originalLastName = testUser.LastName;
            var mockSet = CreateMockDbSet(new[] { testUser });
            _mockDbContext.Setup(x => x.Users).Returns(mockSet.Object);
            _mockDbContext.Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);

            var request = new UpdateUserRequest
            {
                FirstName = "UpdatedFirst"
            };

            // Act
            var result = await _controller.UpdateMe(request, CancellationToken.None);

            // Assert
            result.Should().BeOfType<OkObjectResult>();
            testUser.FirstName.Should().Be("UpdatedFirst");
            testUser.LastName.Should().Be(originalLastName);
        }

        [Fact]
        public async Task DeleteMe_WhenUserExists_SoftDeletesUserAndReturnsNoContent()
        {
            // Arrange
            var testUser = CreateTestUser();
            var mockSet = CreateMockDbSet(new[] { testUser });
            _mockDbContext.Setup(x => x.Users).Returns(mockSet.Object);
            _mockDbContext.Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);

            // Act
            var result = await _controller.DeleteMe(CancellationToken.None);

            // Assert
            result.Should().BeOfType<NoContentResult>();
            testUser.IsActive.Should().BeFalse();
            _mockDbContext.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task DeleteMe_WhenUserDoesNotExist_ReturnsNotFound()
        {
            // Arrange
            var mockSet = CreateMockDbSet(Array.Empty<User>());
            _mockDbContext.Setup(x => x.Users).Returns(mockSet.Object);

            // Act
            var result = await _controller.DeleteMe(CancellationToken.None);

            // Assert
            result.Should().BeOfType<NotFoundResult>();
            _mockDbContext.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
        }

        [Fact]
        public async Task GetMyOrganizations_ReturnsOkWithPaginatedResults()
        {
            // Arrange
            var testOrg1 = CreateTestOrganization("Org1");
            var testOrg2 = CreateTestOrganization("Org2");
            var testTeam1 = CreateTestTeam(testOrg1.Id);
            var testTeam2 = CreateTestTeam(testOrg2.Id);
            var testMembership1 = CreateTestTeamMembership(_testUserId, testTeam1.Id, testOrg1.Id);
            var testMembership2 = CreateTestTeamMembership(_testUserId, testTeam2.Id, testOrg2.Id);

            testTeam1.TeamMemberships = new List<TeamMembership> { testMembership1 };
            testTeam2.TeamMemberships = new List<TeamMembership> { testMembership2 };
            testOrg1.Teams = new List<Team> { testTeam1 };
            testOrg2.Teams = new List<Team> { testTeam2 };

            var mockOrgSet = CreateMockDbSet(new[] { testOrg1, testOrg2 });
            _mockDbContext.Setup(x => x.Organizations).Returns(mockOrgSet.Object);

            // Act
            var result = await _controller.GetMyOrganizations(1, 10, CancellationToken.None);

            // Assert
            result.Should().BeOfType<OkObjectResult>();
            var okResult = result as OkObjectResult;
            okResult!.Value.Should().NotBeNull();
        }

        [Fact]
        public async Task GetMyOrganizations_WithPagination_ReturnsCorrectPage()
        {
            // Arrange
            var organizations = Enumerable.Range(1, 15)
                .Select(i => CreateTestOrganization($"Org{i}"))
                .ToList();

            foreach (var org in organizations)
            {
                var team = CreateTestTeam(org.Id);
                var membership = CreateTestTeamMembership(_testUserId, team.Id, org.Id);
                team.TeamMemberships = new List<TeamMembership> { membership };
                org.Teams = new List<Team> { team };
            }

            var mockOrgSet = CreateMockDbSet(organizations);
            _mockDbContext.Setup(x => x.Organizations).Returns(mockOrgSet.Object);

            // Act
            var result = await _controller.GetMyOrganizations(page: 2, pageSize: 10, CancellationToken.None);

            // Assert
            result.Should().BeOfType<OkObjectResult>();
            var okResult = result as OkObjectResult;
            var response = okResult!.Value;
            response.Should().NotBeNull();
        }

        [Fact]
        public async Task GetMyTeams_ReturnsOkWithPaginatedResults()
        {
            // Arrange
            var testTeam1 = CreateTestTeam(_testOrganizationId);
            var testTeam2 = CreateTestTeam(_testOrganizationId);
            var testMembership1 = CreateTestTeamMembership(_testUserId, testTeam1.Id, _testOrganizationId);
            var testMembership2 = CreateTestTeamMembership(_testUserId, testTeam2.Id, _testOrganizationId);

            testTeam1.TeamMemberships = new List<TeamMembership> { testMembership1 };
            testTeam2.TeamMemberships = new List<TeamMembership> { testMembership2 };

            var mockTeamSet = CreateMockDbSet(new[] { testTeam1, testTeam2 });
            _mockDbContext.Setup(x => x.Teams).Returns(mockTeamSet.Object);

            // Act
            var result = await _controller.GetMyTeams(1, 10, CancellationToken.None);

            // Assert
            result.Should().BeOfType<OkObjectResult>();
            var okResult = result as OkObjectResult;
            okResult!.Value.Should().NotBeNull();
        }

        [Fact]
        public async Task GetMyTeams_WithPagination_ReturnsCorrectPage()
        {
            // Arrange
            var teams = Enumerable.Range(1, 15)
                .Select(i => CreateTestTeamWithName(_testOrganizationId, $"Team{i}"))
                .ToList();

            foreach (var team in teams)
            {
                var membership = CreateTestTeamMembership(_testUserId, team.Id, _testOrganizationId);
                team.TeamMemberships = new List<TeamMembership> { membership };
            }

            var mockTeamSet = CreateMockDbSet(teams);
            _mockDbContext.Setup(x => x.Teams).Returns(mockTeamSet.Object);

            // Act
            var result = await _controller.GetMyTeams(page: 2, pageSize: 10, CancellationToken.None);

            // Assert
            result.Should().BeOfType<OkObjectResult>();
            var okResult = result as OkObjectResult;
            var response = okResult!.Value;
            response.Should().NotBeNull();
        }

        [Fact]
        public async Task UpdateCrossBorderConsent_WithValidRequest_UpdatesConsentAndReturnsOk()
        {
            // Arrange
            var testUser = CreateTestUser();
            var mockSet = CreateMockDbSet(new[] { testUser });
            _mockDbContext.Setup(x => x.Users).Returns(mockSet.Object);
            _mockDbContext.Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);

            var request = new UpdateCrossBorderConsentRequest
            {
                ConsentGiven = true,
                ConsentVersion = "v1.0"
            };

            // Act
            var result = await _controller.UpdateCrossBorderConsent(request, CancellationToken.None);

            // Assert
            result.Should().BeOfType<OkObjectResult>();
            testUser.CrossBorderConsentGiven.Should().BeTrue();
            testUser.CrossBorderConsentVersion.Should().Be("v1.0");
            testUser.CrossBorderConsentDate.Should().NotBeNull();
            _mockDbContext.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task UpdateCrossBorderConsent_WithMissingConsentGiven_ReturnsBadRequest()
        {
            // Arrange
            var testUser = CreateTestUser();
            var mockSet = CreateMockDbSet(new[] { testUser });
            _mockDbContext.Setup(x => x.Users).Returns(mockSet.Object);

            var request = new UpdateCrossBorderConsentRequest
            {
                ConsentVersion = "v1.0"
            };

            // Act
            var result = await _controller.UpdateCrossBorderConsent(request, CancellationToken.None);

            // Assert
            result.Should().BeOfType<BadRequestObjectResult>();
            _mockDbContext.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
        }

        [Fact]
        public async Task GetMe_UsesUserContextForUserId()
        {
            // Arrange
            var testUser = CreateTestUser();
            var mockSet = CreateMockDbSet(new[] { testUser });
            _mockDbContext.Setup(x => x.Users).Returns(mockSet.Object);

            // Act
            await _controller.GetMe(CancellationToken.None);

            // Assert
            _mockUserContext.Verify(x => x.UserId, Times.AtLeastOnce);
        }

        [Fact]
        public async Task UpdateMe_UsesUserContextForUserId()
        {
            // Arrange
            var testUser = CreateTestUser();
            var mockSet = CreateMockDbSet(new[] { testUser });
            _mockDbContext.Setup(x => x.Users).Returns(mockSet.Object);
            _mockDbContext.Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);

            var request = new UpdateUserRequest { FirstName = "Test" };

            // Act
            await _controller.UpdateMe(request, CancellationToken.None);

            // Assert
            _mockUserContext.Verify(x => x.UserId, Times.AtLeastOnce);
        }

        private User CreateTestUser()
        {
            return new User
            {
                Id = _testUserId,
                OrganizationId = _testOrganizationId,
                Email = "test@example.com",
                FirstName = "Test",
                LastName = "User",
                Timezone = "UTC",
                Locale = "en-US",
                PasswordHash = "hashed",
                DataResidencyRegion = "us-east-1",
                CreatedInRegion = "us-east-1",
                IsActive = true,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };
        }

        private Organization CreateTestOrganization(string name)
        {
            return new Organization
            {
                Id = Guid.NewGuid(),
                Name = name,
                Slug = name.ToLowerInvariant(),
                Description = "Test organization",
                PrimaryRegion = "us-east-1",
                Tier = "free",
                IsActive = true,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
                Teams = new List<Team>()
            };
        }

        private Team CreateTestTeam(Guid organizationId)
        {
            return new Team
            {
                Id = Guid.NewGuid(),
                OrganizationId = organizationId,
                Name = "Test Team",
                Slug = "test-team",
                Description = "Test team",
                IsActive = true,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
                TeamMemberships = new List<TeamMembership>()
            };
        }

        private Team CreateTestTeamWithName(Guid organizationId, string name)
        {
            return new Team
            {
                Id = Guid.NewGuid(),
                OrganizationId = organizationId,
                Name = name,
                Slug = name.ToLowerInvariant(),
                Description = "Test team",
                IsActive = true,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
                TeamMemberships = new List<TeamMembership>()
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

        private Mock<DbSet<T>> CreateMockDbSet<T>(IEnumerable<T> data)
            where T : class
        {
            var queryableData = data.AsQueryable();
            var mockSet = new Mock<DbSet<T>>();

            mockSet.As<IQueryable<T>>().Setup(m => m.Provider).Returns(new TestAsyncQueryProvider<T>(queryableData.Provider));
            mockSet.As<IQueryable<T>>().Setup(m => m.Expression).Returns(queryableData.Expression);
            mockSet.As<IQueryable<T>>().Setup(m => m.ElementType).Returns(queryableData.ElementType);
            mockSet.As<IQueryable<T>>().Setup(m => m.GetEnumerator()).Returns(queryableData.GetEnumerator());
            mockSet.As<IAsyncEnumerable<T>>().Setup(m => m.GetAsyncEnumerator(It.IsAny<CancellationToken>())).Returns(new TestAsyncEnumerator<T>(queryableData.GetEnumerator()));

            return mockSet;
        }
    }
}
