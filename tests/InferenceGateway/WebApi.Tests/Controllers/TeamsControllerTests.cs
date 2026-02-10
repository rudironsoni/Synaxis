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
    public class TeamsControllerTests
    {
        private readonly Mock<SynaxisDbContext> _mockDbContext;
        private readonly Mock<IAuditService> _mockAuditService;
        private readonly TeamsController _controller;
        private readonly Guid _testUserId = Guid.NewGuid();
        private readonly Guid _testOrganizationId = Guid.NewGuid();
        private readonly Guid _testTeamId = Guid.NewGuid();

        public TeamsControllerTests()
        {
            var options = new DbContextOptionsBuilder<SynaxisDbContext>()
                .UseInMemoryDatabase(databaseName: $"TestDb_{Guid.NewGuid()}")
                .Options;
            _mockDbContext = new Mock<SynaxisDbContext>(options);
            _mockAuditService = new Mock<IAuditService>();

            _controller = new TeamsController(_mockDbContext.Object, _mockAuditService.Object);
            SetupControllerContext();
        }

        [Fact]
        public async Task CreateTeam_WhenUserNotInOrganization_ReturnsForbid()
        {
            // Arrange
            var request = new CreateTeamRequest { Name = "Test Team" };
            var mockUserSet = CreateMockDbSet(Array.Empty<User>());
            _mockDbContext.Setup(x => x.Users).Returns(mockUserSet.Object);

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
            var mockUserSet = CreateMockDbSet(new[] { user });
            _mockDbContext.Setup(x => x.Users).Returns(mockUserSet.Object);

            var mockOrgSet = CreateMockDbSet(Array.Empty<Organization>());
            _mockDbContext.Setup(x => x.Organizations).Returns(mockOrgSet.Object);

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
            var mockUserSet = CreateMockDbSet(new[] { user });
            _mockDbContext.Setup(x => x.Users).Returns(mockUserSet.Object);

            var org = CreateTestOrganization();
            var mockOrgSet = CreateMockDbSet(new[] { org });
            _mockDbContext.Setup(x => x.Organizations).Returns(mockOrgSet.Object);

            var existingTeam = new Team { Id = Guid.NewGuid(), OrganizationId = _testOrganizationId, Name = "Test Team" };
            var mockTeamSet = CreateMockDbSet(new[] { existingTeam });
            _mockDbContext.Setup(x => x.Teams).Returns(mockTeamSet.Object);

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
            var mockUserSet = CreateMockDbSet(new[] { user });
            _mockDbContext.Setup(x => x.Users).Returns(mockUserSet.Object);

            var org = CreateTestOrganization();
            var mockOrgSet = CreateMockDbSet(new[] { org });
            _mockDbContext.Setup(x => x.Organizations).Returns(mockOrgSet.Object);

            var mockTeamSet = CreateMockDbSet(Array.Empty<Team>());
            _mockDbContext.Setup(x => x.Teams).Returns(mockTeamSet.Object);
            _mockDbContext.Setup(x => x.Teams.Add(It.IsAny<Team>()));

            var mockMembershipSet = CreateMockDbSet(Array.Empty<TeamMembership>());
            _mockDbContext.Setup(x => x.TeamMemberships).Returns(mockMembershipSet.Object);
            _mockDbContext.Setup(x => x.TeamMemberships.Add(It.IsAny<TeamMembership>()));

            _mockDbContext.Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);

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

            // Verify DbContext interactions
            _mockDbContext.Verify(x => x.Teams.Add(It.IsAny<Team>()), Times.Once);
            _mockDbContext.Verify(x => x.TeamMemberships.Add(It.IsAny<TeamMembership>()), Times.Once);
            _mockDbContext.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);

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
            var mockMembershipSet = CreateMockDbSet(Array.Empty<TeamMembership>());
            _mockDbContext.Setup(x => x.TeamMemberships).Returns(mockMembershipSet.Object);

            var mockUserSet = CreateMockDbSet(Array.Empty<User>());
            _mockDbContext.Setup(x => x.Users).Returns(mockUserSet.Object);

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

            var mockTeamSet = CreateMockDbSet(Array.Empty<Team>());
            _mockDbContext.Setup(x => x.Teams).Returns(mockTeamSet.Object);

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
            var mockTeamSet = CreateMockDbSet(new[] { team });
            _mockDbContext.Setup(x => x.Teams).Returns(mockTeamSet.Object);

            var mockUserSet = CreateMockDbSet(Array.Empty<User>());
            _mockDbContext.Setup(x => x.Users).Returns(mockUserSet.Object);

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
            var mockTeamSet = CreateMockDbSet(new[] { team });
            _mockDbContext.Setup(x => x.Teams).Returns(mockTeamSet.Object);

            var userToAdd = new User { Id = userIdToAdd, OrganizationId = Guid.NewGuid() }; // Different org
            var mockUserSet = CreateMockDbSet(new[] { userToAdd });
            _mockDbContext.Setup(x => x.Users).Returns(mockUserSet.Object);

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
            var mockTeamSet = CreateMockDbSet(new[] { team });
            _mockDbContext.Setup(x => x.Teams).Returns(mockTeamSet.Object);

            var userToAdd = new User { Id = userIdToAdd, OrganizationId = _testOrganizationId };
            var existingMembership = new TeamMembership { UserId = userIdToAdd, TeamId = _testTeamId };
            var mockUserSet = CreateMockDbSet(new[] { userToAdd });
            _mockDbContext.Setup(x => x.Users).Returns(mockUserSet.Object);

            var mockMembershipSet = CreateMockDbSet(new[] { existingMembership });
            _mockDbContext.Setup(x => x.TeamMemberships).Returns(mockMembershipSet.Object);

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
            var mockTeamSet = CreateMockDbSet(new[] { team });
            _mockDbContext.Setup(x => x.Teams).Returns(mockTeamSet.Object);

            var userToAdd = new User { Id = userIdToAdd, OrganizationId = _testOrganizationId };
            var mockUserSet = CreateMockDbSet(new[] { userToAdd });
            _mockDbContext.Setup(x => x.Users).Returns(mockUserSet.Object);

            var mockMembershipSet = CreateMockDbSet(Array.Empty<TeamMembership>());
            _mockDbContext.Setup(x => x.TeamMemberships).Returns(mockMembershipSet.Object);
            _mockDbContext.Setup(x => x.TeamMemberships.Add(It.IsAny<TeamMembership>()));

            _mockDbContext.Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);

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

            // Verify DbContext interactions
            _mockDbContext.Verify(x => x.TeamMemberships.Add(It.IsAny<TeamMembership>()), Times.Once);
            _mockDbContext.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);

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
            var mockMembershipSet = CreateMockDbSet(Array.Empty<TeamMembership>());
            _mockDbContext.Setup(x => x.TeamMemberships).Returns(mockMembershipSet.Object);

            var mockUserSet = CreateMockDbSet(Array.Empty<User>());
            _mockDbContext.Setup(x => x.Users).Returns(mockUserSet.Object);

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

            var mockTeamSet = CreateMockDbSet(Array.Empty<Team>());
            _mockDbContext.Setup(x => x.Teams).Returns(mockTeamSet.Object);

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
            var mockTeamSet = CreateMockDbSet(new[] { team });
            _mockDbContext.Setup(x => x.Teams).Returns(mockTeamSet.Object);

            var mockMembershipSet = CreateMockDbSet(Array.Empty<TeamMembership>());
            _mockDbContext.Setup(x => x.TeamMemberships).Returns(mockMembershipSet.Object);

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
            var mockTeamSet = CreateMockDbSet(new[] { team });
            _mockDbContext.Setup(x => x.Teams).Returns(mockTeamSet.Object);

            var membership = new TeamMembership
            {
                Id = Guid.NewGuid(),
                UserId = memberId,
                TeamId = _testTeamId,
                Role = "Member",
                JoinedAt = DateTime.UtcNow
            };
            var mockMembershipSet = CreateMockDbSet(new[] { membership });
            _mockDbContext.Setup(x => x.TeamMemberships).Returns(mockMembershipSet.Object);

            _mockDbContext.Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);

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

            membership.Role.Should().Be("TeamAdmin");

            // Verify DbContext interactions
            _mockDbContext.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);

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
                Role = "TeamAdmin"
            };
            var user = CreateTestUser();
            user.Role = "admin";

            var mockMembershipSet = CreateMockDbSet(new[] { membership });
            _mockDbContext.Setup(x => x.TeamMemberships).Returns(mockMembershipSet.Object);

            var mockUserSet = CreateMockDbSet(new[] { user });
            _mockDbContext.Setup(x => x.Users).Returns(mockUserSet.Object);
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
