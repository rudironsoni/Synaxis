// <copyright file="TeamsControllerTests.cs" company="Synaxis">
// Copyright (c) Synaxis. All rights reserved.
// </copyright>

namespace Synaxis.InferenceGateway.WebApi.Tests.Controllers
{
    using System;
    using System.Collections.Generic;
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
    using Synaxis.InferenceGateway.Application.Security;
    using Synaxis.Infrastructure.Data;
    using Synaxis.InferenceGateway.WebApi.Controllers;
    using Xunit;

    [Trait("Category", "Unit")]
    public class TeamsControllerTests : IDisposable
    {
        private readonly SynaxisDbContext _dbContext;
        private readonly Mock<IAuditService> _mockAuditService;
        private readonly TeamsController _controller;
        private readonly Guid _testUserId = Guid.NewGuid();
        private readonly Guid _testOrganizationId = Guid.NewGuid();
        private readonly Guid _testTeamId = Guid.NewGuid();
        private bool _disposed;

        public TeamsControllerTests()
        {
            var options = new DbContextOptionsBuilder<SynaxisDbContext>()
                .UseInMemoryDatabase(databaseName: $"TestDb_{Guid.NewGuid()}")
                .Options;
            _dbContext = new SynaxisDbContext(options);
            _mockAuditService = new Mock<IAuditService>();

            _controller = new TeamsController(_dbContext, _mockAuditService.Object);
            SetupControllerContext();
        }

        public void Dispose()
        {
            if (!_disposed)
            {
                _dbContext.Dispose();
                _disposed = true;
            }
        }

        [Fact]
        public async Task CreateTeam_WhenUserNotInOrganization_ReturnsForbid()
        {
            // Arrange
            var request = new CreateTeamRequest { Name = "Test Team" };

            // Act
            var result = await _controller.CreateTeam(_testOrganizationId, request, CancellationToken.None);

            // Assert
            result.Should().BeOfType<ForbidResult>();
        }

        [Fact]
        public async Task CreateTeam_WhenOrganizationDoesNotExist_ReturnsNotFound()
        {
            // Arrange
            var request = new CreateTeamRequest { Name = "Test Team" };
            var user = CreateTestUser();
            _dbContext.Users.Add(user);
            await _dbContext.SaveChangesAsync();

            // Act
            var result = await _controller.CreateTeam(_testOrganizationId, request, CancellationToken.None);

            // Assert
            result.Should().BeOfType<NotFoundObjectResult>();
            var notFoundResult = result as NotFoundObjectResult;
            notFoundResult!.Value.Should().Be("Organization not found");
        }

        [Fact]
        public async Task CreateTeam_WhenTeamNameAlreadyExists_ReturnsBadRequest()
        {
            // Arrange
            var request = new CreateTeamRequest { Name = "Test Team" };
            var user = CreateTestUser();
            var org = CreateTestOrganization();
            var existingTeam = new Team { Id = Guid.NewGuid(), OrganizationId = _testOrganizationId, Name = "Test Team", Slug = "test-team" };

            _dbContext.Users.Add(user);
            _dbContext.Organizations.Add(org);
            _dbContext.Teams.Add(existingTeam);
            await _dbContext.SaveChangesAsync();

            // Act
            var result = await _controller.CreateTeam(_testOrganizationId, request, CancellationToken.None);

            // Assert
            result.Should().BeOfType<BadRequestObjectResult>();
            var badRequestResult = result as BadRequestObjectResult;
            badRequestResult!.Value.Should().Be("A team with this name already exists in the organization");
        }

        [Fact]
        public async Task CreateTeam_WithValidRequest_ReturnsCreatedResult()
        {
            // Arrange
            var request = new CreateTeamRequest { Name = "Test Team", Description = "Test Description" };
            var user = CreateTestUser();
            var org = CreateTestOrganization();

            _dbContext.Users.Add(user);
            _dbContext.Organizations.Add(org);
            await _dbContext.SaveChangesAsync();

            _mockAuditService.Setup(x => x.LogAsync(
                It.IsAny<Guid>(),
                It.IsAny<Guid>(),
                It.IsAny<string>(),
                It.IsAny<object>(),
                It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask);

            // Act
            var result = await _controller.CreateTeam(_testOrganizationId, request, CancellationToken.None);

            // Assert
            result.Should().BeOfType<CreatedAtActionResult>();
            var createdResult = result as CreatedAtActionResult;
            createdResult!.ActionName.Should().Be(nameof(TeamsController.GetTeam));

            var value = createdResult.Value;
            value.Should().NotBeNull();

            // Verify team was created
            var teams = await _dbContext.Teams.ToListAsync();
            teams.Should().HaveCount(1);
            teams[0].Name.Should().Be("Test Team");

            // Verify membership was created
            var memberships = await _dbContext.TeamMemberships.ToListAsync();
            memberships.Should().HaveCount(1);
            memberships[0].UserId.Should().Be(_testUserId);
            memberships[0].TeamId.Should().Be(teams[0].Id);

            // Verify audit log
            _mockAuditService.Verify(x => x.LogAsync(
                _testOrganizationId,
                _testUserId,
                "CreateTeam",
                It.IsAny<object>(),
                It.IsAny<CancellationToken>()),
                Times.Once);
        }

        private User CreateTestUser()
        {
            return new User
            {
                Id = _testUserId,
                OrganizationId = _testOrganizationId,
                Email = "test@example.com",
                PasswordHash = "hashedpassword",
                Role = "member",
                DataResidencyRegion = "us-east-1",
                CreatedInRegion = "us-east-1",
                IsActive = true,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
            };
        }

        private Organization CreateTestOrganization()
        {
            return new Organization
            {
                Id = _testOrganizationId,
                Name = "Test Organization",
                Slug = "test-org",
                PrimaryRegion = "us-east-1",
                Tier = "free",
                IsActive = true,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
            };
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
