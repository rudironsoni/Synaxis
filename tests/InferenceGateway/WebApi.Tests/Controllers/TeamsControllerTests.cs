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

        [Fact]
        public async Task AddMember_WhenUserNotTeamAdmin_ReturnsForbid()
        {
            // Arrange
            var request = new AddMemberRequest { UserId = Guid.NewGuid(), Role = "member" };

            // Act
            var result = await _controller.AddMember(_testOrganizationId, _testTeamId, request, CancellationToken.None);

            // Assert
            result.Should().BeOfType<ForbidResult>();
        }

        [Fact]
        public async Task AddMember_WhenRoleIsInvalid_ReturnsBadRequest()
        {
            // Arrange
            var request = new AddMemberRequest { UserId = Guid.NewGuid(), Role = "invalid_role" };
            SetupTeamAdminPermission();

            // Act
            var result = await _controller.AddMember(_testOrganizationId, _testTeamId, request, CancellationToken.None);

            // Assert
            result.Should().BeOfType<BadRequestObjectResult>();
            var badRequestResult = result as BadRequestObjectResult;
            badRequestResult!.Value.Should().Be("Role must be 'admin' or 'member'");
        }

        [Fact]
        public async Task AddMember_WhenTeamDoesNotExist_ReturnsNotFound()
        {
            // Arrange
            var request = new AddMemberRequest { UserId = Guid.NewGuid(), Role = "member" };
            SetupTeamAdminPermission();

            // Act
            var result = await _controller.AddMember(_testOrganizationId, _testTeamId, request, CancellationToken.None);

            // Assert
            result.Should().BeOfType<NotFoundObjectResult>();
            var notFoundResult = result as NotFoundObjectResult;
            notFoundResult!.Value.Should().Be("Team not found");
        }

        [Fact]
        public async Task AddMember_WhenUserDoesNotExist_ReturnsBadRequest()
        {
            // Arrange
            var userIdToAdd = Guid.NewGuid();
            var request = new AddMemberRequest { UserId = userIdToAdd, Role = "member" };
            SetupTeamAdminPermission();

            var team = CreateTestTeam();
            _dbContext.Teams.Add(team);
            await _dbContext.SaveChangesAsync();

            // Act
            var result = await _controller.AddMember(_testOrganizationId, _testTeamId, request, CancellationToken.None);

            // Assert
            result.Should().BeOfType<BadRequestObjectResult>();
            var badRequestResult = result as BadRequestObjectResult;
            badRequestResult!.Value.Should().Be("User not found");
        }

        [Fact]
        public async Task AddMember_WhenUserNotInOrganization_ReturnsBadRequest()
        {
            // Arrange
            var userIdToAdd = Guid.NewGuid();
            var request = new AddMemberRequest { UserId = userIdToAdd, Role = "member" };
            SetupTeamAdminPermission();

            var team = CreateTestTeam();
            var userToAdd = new User { Id = userIdToAdd, OrganizationId = Guid.NewGuid() }; // Different org

            _dbContext.Teams.Add(team);
            _dbContext.Users.Add(userToAdd);
            await _dbContext.SaveChangesAsync();

            // Act
            var result = await _controller.AddMember(_testOrganizationId, _testTeamId, request, CancellationToken.None);

            // Assert
            result.Should().BeOfType<BadRequestObjectResult>();
            var badRequestResult = result as BadRequestObjectResult;
            badRequestResult!.Value.Should().Be("User is not a member of the organization");
        }

        [Fact]
        public async Task AddMember_WhenUserAlreadyMember_ReturnsBadRequest()
        {
            // Arrange
            var userIdToAdd = Guid.NewGuid();
            var request = new AddMemberRequest { UserId = userIdToAdd, Role = "member" };
            SetupTeamAdminPermission();

            var team = CreateTestTeam();
            var userToAdd = new User { Id = userIdToAdd, OrganizationId = _testOrganizationId };
            var existingMembership = new TeamMembership { UserId = userIdToAdd, TeamId = _testTeamId, OrganizationId = _testOrganizationId };

            _dbContext.Teams.Add(team);
            _dbContext.Users.Add(userToAdd);
            _dbContext.TeamMemberships.Add(existingMembership);
            await _dbContext.SaveChangesAsync();

            // Act
            var result = await _controller.AddMember(_testOrganizationId, _testTeamId, request, CancellationToken.None);

            // Assert
            result.Should().BeOfType<BadRequestObjectResult>();
            var badRequestResult = result as BadRequestObjectResult;
            badRequestResult!.Value.Should().Be("User is already a member of the team");
        }

        [Fact]
        public async Task AddMember_WithValidRequest_ReturnsCreatedStatus()
        {
            // Arrange
            var userIdToAdd = Guid.NewGuid();
            var request = new AddMemberRequest { UserId = userIdToAdd, Role = "member" };
            SetupTeamAdminPermission();

            var team = CreateTestTeam();
            var userToAdd = new User { Id = userIdToAdd, OrganizationId = _testOrganizationId };

            _dbContext.Teams.Add(team);
            _dbContext.Users.Add(userToAdd);
            await _dbContext.SaveChangesAsync();

            _mockAuditService.Setup(x => x.LogAsync(
                It.IsAny<Guid>(),
                It.IsAny<Guid>(),
                It.IsAny<string>(),
                It.IsAny<object>(),
                It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask);

            // Act
            var result = await _controller.AddMember(_testOrganizationId, _testTeamId, request, CancellationToken.None);

            // Assert
            result.Should().BeOfType<StatusCodeResult>();
            var statusResult = result as StatusCodeResult;
            statusResult!.StatusCode.Should().Be(201);

            // Verify membership was created
            var memberships = await _dbContext.TeamMemberships.ToListAsync();
            memberships.Should().HaveCount(2); // 1 from SetupTeamAdminPermission + 1 new
            memberships.Should().Contain(m => m.UserId == userIdToAdd && m.TeamId == _testTeamId);

            // Verify audit log
            _mockAuditService.Verify(x => x.LogAsync(
                _testOrganizationId,
                _testUserId,
                "AddTeamMember",
                It.IsAny<object>(),
                It.IsAny<CancellationToken>()),
                Times.Once);
        }

        [Fact]
        public async Task UpdateMemberRole_WhenUserNotTeamAdmin_ReturnsForbid()
        {
            // Arrange
            var memberId = Guid.NewGuid();
            var request = new Synaxis.InferenceGateway.WebApi.Controllers.UpdateMemberRoleRequest { Role = "admin" };

            // Act
            var result = await _controller.UpdateMemberRole(_testOrganizationId, _testTeamId, memberId, request, CancellationToken.None);

            // Assert
            result.Should().BeOfType<ForbidResult>();
        }

        [Fact]
        public async Task UpdateMemberRole_WhenRoleIsInvalid_ReturnsBadRequest()
        {
            // Arrange
            var memberId = Guid.NewGuid();
            var request = new Synaxis.InferenceGateway.WebApi.Controllers.UpdateMemberRoleRequest { Role = "invalid_role" };
            SetupTeamAdminPermission();

            // Act
            var result = await _controller.UpdateMemberRole(_testOrganizationId, _testTeamId, memberId, request, CancellationToken.None);

            // Assert
            result.Should().BeOfType<BadRequestObjectResult>();
            var badRequestResult = result as BadRequestObjectResult;
            badRequestResult!.Value.Should().Be("Role must be 'admin' or 'member'");
        }

        [Fact]
        public async Task UpdateMemberRole_WhenTeamDoesNotExist_ReturnsNotFound()
        {
            // Arrange
            var memberId = Guid.NewGuid();
            var request = new Synaxis.InferenceGateway.WebApi.Controllers.UpdateMemberRoleRequest { Role = "admin" };
            SetupTeamAdminPermission();

            // Act
            var result = await _controller.UpdateMemberRole(_testOrganizationId, _testTeamId, memberId, request, CancellationToken.None);

            // Assert
            result.Should().BeOfType<NotFoundObjectResult>();
            var notFoundResult = result as NotFoundObjectResult;
            notFoundResult!.Value.Should().Be("Team not found");
        }

        [Fact]
        public async Task UpdateMemberRole_WhenMemberNotFound_ReturnsNotFound()
        {
            // Arrange
            var memberId = Guid.NewGuid();
            var request = new Synaxis.InferenceGateway.WebApi.Controllers.UpdateMemberRoleRequest { Role = "admin" };
            SetupTeamAdminPermission();

            var team = CreateTestTeam();
            _dbContext.Teams.Add(team);
            await _dbContext.SaveChangesAsync();

            // Act
            var result = await _controller.UpdateMemberRole(_testOrganizationId, _testTeamId, memberId, request, CancellationToken.None);

            // Assert
            result.Should().BeOfType<NotFoundObjectResult>();
            var notFoundResult = result as NotFoundObjectResult;
            notFoundResult!.Value.Should().Be("Member not found in team");
        }

        [Fact]
        public async Task UpdateMemberRole_WithValidRequest_ReturnsOkWithUpdatedRole()
        {
            // Arrange
            var memberId = Guid.NewGuid();
            var request = new Synaxis.InferenceGateway.WebApi.Controllers.UpdateMemberRoleRequest { Role = "admin" };
            SetupTeamAdminPermission();

            var team = CreateTestTeam();
            var membership = new TeamMembership
            {
                Id = Guid.NewGuid(),
                UserId = memberId,
                TeamId = _testTeamId,
                OrganizationId = _testOrganizationId,
                Role = "Member",
                JoinedAt = DateTime.UtcNow
            };

            _dbContext.Teams.Add(team);
            _dbContext.TeamMemberships.Add(membership);
            await _dbContext.SaveChangesAsync();

            _mockAuditService.Setup(x => x.LogAsync(
                It.IsAny<Guid>(),
                It.IsAny<Guid>(),
                It.IsAny<string>(),
                It.IsAny<object>(),
                It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask);

            // Act
            var result = await _controller.UpdateMemberRole(_testOrganizationId, _testTeamId, memberId, request, CancellationToken.None);

            // Assert
            result.Should().BeOfType<OkObjectResult>();
            var okResult = result as OkObjectResult;
            okResult!.Value.Should().NotBeNull();

            // Refresh from database
            await _dbContext.Entry(membership).ReloadAsync();
            membership.Role.Should().Be("TeamAdmin");

            // Verify audit log
            _mockAuditService.Verify(x => x.LogAsync(
                _testOrganizationId,
                _testUserId,
                "UpdateTeamMemberRole",
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
                Role = "member"
            };
        }

        private Organization CreateTestOrganization()
        {
            return new Organization
            {
                Id = _testOrganizationId,
                Name = "Test Organization",
                Slug = "test-org",
                IsActive = true
            };
        }

        private Team CreateTestTeam()
        {
            return new Team
            {
                Id = _testTeamId,
                OrganizationId = _testOrganizationId,
                Name = "Test Team",
                Slug = "test-team",
                IsActive = true
            };
        }

        private void SetupTeamAdminPermission()
        {
            var membership = new TeamMembership
            {
                UserId = _testUserId,
                TeamId = _testTeamId,
                OrganizationId = _testOrganizationId,
                Role = "TeamAdmin"
            };
            var user = CreateTestUser();
            user.Role = "admin";

            _dbContext.TeamMemberships.Add(membership);
            _dbContext.Users.Add(user);
            _dbContext.SaveChanges();
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
