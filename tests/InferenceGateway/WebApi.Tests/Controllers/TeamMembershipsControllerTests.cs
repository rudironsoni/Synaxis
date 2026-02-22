// <copyright file="TeamMembershipsControllerTests.cs" company="Synaxis">
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
    public class TeamMembershipsControllerTests : IDisposable
    {
        private readonly SynaxisDbContext _dbContext;
        private readonly Mock<IAuditService> _mockAuditService;
        private readonly TeamMembershipsController _controller;
        private readonly Guid _testUserId = Guid.NewGuid();
        private readonly Guid _testOrganizationId = Guid.NewGuid();
        private readonly Guid _testTeamId = Guid.NewGuid();
        private bool _disposed;

        public TeamMembershipsControllerTests()
        {
            var options = new DbContextOptionsBuilder<SynaxisDbContext>()
                .UseInMemoryDatabase(databaseName: $"TestDb_{Guid.NewGuid()}")
                .Options;
            _dbContext = new SynaxisDbContext(options);
            _mockAuditService = new Mock<IAuditService>();

            _controller = new TeamMembershipsController(_dbContext, _mockAuditService.Object);
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
        public async Task AddMember_WhenUserNotTeamAdmin_ReturnsForbid()
        {
            // Arrange
            var request = new AddTeamMemberRequest { UserId = Guid.NewGuid(), Role = "member" };

            // Act
            var result = await _controller.AddMember(_testOrganizationId, _testTeamId, request, CancellationToken.None);

            // Assert
            result.Should().BeOfType<ForbidResult>();
        }

        [Fact]
        public async Task AddMember_WhenRoleIsInvalid_ReturnsBadRequest()
        {
            // Arrange
            var request = new AddTeamMemberRequest { UserId = Guid.NewGuid(), Role = "invalid_role" };
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
            var request = new AddTeamMemberRequest { UserId = Guid.NewGuid(), Role = "member" };
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
            var request = new AddTeamMemberRequest { UserId = userIdToAdd, Role = "member" };
            SetupTeamAdminPermission();

            var team = CreateTestTeam();
            _dbContext.Teams.Add(team);
            await _dbContext.SaveChangesAsync();

            // Act
            var result = await _controller.AddMember(_testOrganizationId, _testTeamId, request, CancellationToken.None);

            // Assert
            result.Should().BeOfType<BadRequestObjectResult>();
            var badRequestResult = result as BadRequestObjectResult;
            badRequestResult!.Value.Should().Be("User is not a member of this organization");
        }

        [Fact]
        public async Task AddMember_WhenUserNotInOrganization_ReturnsBadRequest()
        {
            // Arrange
            var userIdToAdd = Guid.NewGuid();
            var request = new AddTeamMemberRequest { UserId = userIdToAdd, Role = "member" };
            SetupTeamAdminPermission();

            var team = CreateTestTeam();
            var userToAdd = new User
            {
                Id = userIdToAdd,
                OrganizationId = Guid.NewGuid(), // Different org
                Email = "other@example.com",
                PasswordHash = "hashedpassword",
                Role = "member",
                DataResidencyRegion = "us-east-1",
                CreatedInRegion = "us-east-1",
                IsActive = true,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
            };

            _dbContext.Teams.Add(team);
            _dbContext.Users.Add(userToAdd);
            await _dbContext.SaveChangesAsync();

            // Act
            var result = await _controller.AddMember(_testOrganizationId, _testTeamId, request, CancellationToken.None);

            // Assert
            result.Should().BeOfType<BadRequestObjectResult>();
            var badRequestResult = result as BadRequestObjectResult;
            badRequestResult!.Value.Should().Be("User is not a member of this organization");
        }

        [Fact]
        public async Task AddMember_WhenUserAlreadyMember_ReturnsBadRequest()
        {
            // Arrange
            var userIdToAdd = Guid.NewGuid();
            var request = new AddTeamMemberRequest { UserId = userIdToAdd, Role = "member" };
            SetupTeamAdminPermission();

            var team = CreateTestTeam();
            var userToAdd = new User
            {
                Id = userIdToAdd,
                OrganizationId = _testOrganizationId,
                Email = "existing@example.com",
                PasswordHash = "hashedpassword",
                Role = "member",
                DataResidencyRegion = "us-east-1",
                CreatedInRegion = "us-east-1",
                IsActive = true,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
            };
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
            badRequestResult!.Value.Should().Be("User is already a member of this team");
        }

        [Fact]
        public async Task AddMember_WithValidRequest_ReturnsCreatedStatus()
        {
            // Arrange
            var userIdToAdd = Guid.NewGuid();
            var request = new AddTeamMemberRequest { UserId = userIdToAdd, Role = "member" };
            SetupTeamAdminPermission();

            var team = CreateTestTeam();
            var userToAdd = new User
            {
                Id = userIdToAdd,
                OrganizationId = _testOrganizationId,
                Email = "newmember@example.com",
                PasswordHash = "hashedpassword",
                Role = "member",
                DataResidencyRegion = "us-east-1",
                CreatedInRegion = "us-east-1",
                IsActive = true,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
            };

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
            result.Should().BeOfType<CreatedAtActionResult>();

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
            var request = new UpdateMemberRoleRequest { Role = "admin" };

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
            var request = new UpdateMemberRoleRequest { Role = "invalid_role" };
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
            var request = new UpdateMemberRoleRequest { Role = "admin" };
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
            var request = new UpdateMemberRoleRequest { Role = "admin" };
            SetupTeamAdminPermission();

            var team = CreateTestTeam();
            _dbContext.Teams.Add(team);
            await _dbContext.SaveChangesAsync();

            // Act
            var result = await _controller.UpdateMemberRole(_testOrganizationId, _testTeamId, memberId, request, CancellationToken.None);

            // Assert
            result.Should().BeOfType<NotFoundObjectResult>();
            var notFoundResult = result as NotFoundObjectResult;
            notFoundResult!.Value.Should().Be("Member not found");
        }

        [Fact]
        public async Task UpdateMemberRole_WithValidRequest_ReturnsOkWithUpdatedRole()
        {
            // Arrange
            var memberId = Guid.NewGuid();
            var request = new UpdateMemberRoleRequest { Role = "admin" };
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

        [Fact]
        public async Task RemoveMember_WhenUserNotTeamAdminAndNotSelf_ReturnsNotFound()
        {
            // Arrange
            var memberId = Guid.NewGuid();

            // Act
            var result = await _controller.RemoveMember(_testOrganizationId, _testTeamId, memberId, CancellationToken.None);

            // Assert
            // Controller first checks for membership, returns NotFound if not found
            result.Should().BeOfType<NotFoundObjectResult>();
            var notFoundResult = result as NotFoundObjectResult;
            notFoundResult!.Value.Should().Be("Member not found");
        }

        [Fact]
        public async Task RemoveMember_WhenMemberNotFound_ReturnsNotFound()
        {
            // Arrange
            var memberId = Guid.NewGuid();
            SetupTeamAdminPermission();

            var team = CreateTestTeam();
            _dbContext.Teams.Add(team);
            await _dbContext.SaveChangesAsync();

            // Act
            var result = await _controller.RemoveMember(_testOrganizationId, _testTeamId, memberId, CancellationToken.None);

            // Assert
            // Controller checks membership first, returns "Member not found"
            result.Should().BeOfType<NotFoundObjectResult>();
            var notFoundResult = result as NotFoundObjectResult;
            notFoundResult!.Value.Should().Be("Member not found");
        }

        [Fact]
        public async Task RemoveMember_WithTeamAdminPermission_RemovesMemberSuccessfully()
        {
            // Arrange
            var memberId = Guid.NewGuid();
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
            var result = await _controller.RemoveMember(_testOrganizationId, _testTeamId, memberId, CancellationToken.None);

            // Assert
            result.Should().BeOfType<NoContentResult>();

            // Verify membership was removed
            var memberships = await _dbContext.TeamMemberships.ToListAsync();
            memberships.Should().HaveCount(1); // Only the admin membership remains
            memberships.Should().NotContain(m => m.UserId == memberId);

            // Verify audit log
            _mockAuditService.Verify(x => x.LogAsync(
                _testOrganizationId,
                _testUserId,
                "RemoveTeamMember",
                It.IsAny<object>(),
                It.IsAny<CancellationToken>()),
                Times.Once);
        }

        [Fact]
        public async Task RemoveMember_WithSelfRemoval_AllowsRemoval()
        {
            // Arrange
            var team = CreateTestTeam();
            var membership = new TeamMembership
            {
                Id = Guid.NewGuid(),
                UserId = _testUserId,
                TeamId = _testTeamId,
                OrganizationId = _testOrganizationId,
                Role = "Member",
                JoinedAt = DateTime.UtcNow
            };

            var user = CreateTestUser();
            _dbContext.Teams.Add(team);
            _dbContext.Users.Add(user);
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
            var result = await _controller.RemoveMember(_testOrganizationId, _testTeamId, _testUserId, CancellationToken.None);

            // Assert
            result.Should().BeOfType<NoContentResult>();

            // Verify membership was removed
            var memberships = await _dbContext.TeamMemberships.ToListAsync();
            memberships.Should().BeEmpty();
        }

        [Fact]
        public async Task ListMembers_WhenUserNotTeamMember_ReturnsForbid()
        {
            // Arrange
            // No team membership set up

            // Act
            var result = await _controller.ListMembers(_testOrganizationId, _testTeamId, 20, 0, CancellationToken.None);

            // Assert
            result.Should().BeOfType<ForbidResult>();
        }

        [Fact]
        public async Task ListMembers_WhenTeamDoesNotExist_ReturnsNotFound()
        {
            // Arrange
            SetupTeamAdminPermission();

            // Act
            var result = await _controller.ListMembers(_testOrganizationId, _testTeamId, 20, 0, CancellationToken.None);

            // Assert
            result.Should().BeOfType<NotFoundObjectResult>();
            var notFoundResult = result as NotFoundObjectResult;
            notFoundResult!.Value.Should().Be("Team not found");
        }

        [Fact]
        public async Task ListMembers_WithValidRequest_ReturnsPaginatedMembers()
        {
            // Arrange
            SetupTeamAdminPermission();

            var team = CreateTestTeam();
            _dbContext.Teams.Add(team);
            await _dbContext.SaveChangesAsync();

            var member1Id = Guid.NewGuid();
            var member2Id = Guid.NewGuid();
            var member3Id = Guid.NewGuid();

            var user1 = new User
            {
                Id = member1Id,
                OrganizationId = _testOrganizationId,
                Email = "member1@example.com",
                PasswordHash = "hashedpassword",
                FirstName = "Member",
                LastName = "One",
                Role = "member",
                DataResidencyRegion = "us-east-1",
                CreatedInRegion = "us-east-1",
                IsActive = true,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
            };
            var user2 = new User
            {
                Id = member2Id,
                OrganizationId = _testOrganizationId,
                Email = "member2@example.com",
                PasswordHash = "hashedpassword",
                FirstName = "Member",
                LastName = "Two",
                Role = "member",
                DataResidencyRegion = "us-east-1",
                CreatedInRegion = "us-east-1",
                IsActive = true,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
            };
            var user3 = new User
            {
                Id = member3Id,
                OrganizationId = _testOrganizationId,
                Email = "member3@example.com",
                PasswordHash = "hashedpassword",
                FirstName = "Member",
                LastName = "Three",
                Role = "member",
                DataResidencyRegion = "us-east-1",
                CreatedInRegion = "us-east-1",
                IsActive = true,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
            };

            var membership1 = new TeamMembership
            {
                Id = Guid.NewGuid(),
                UserId = member1Id,
                TeamId = _testTeamId,
                OrganizationId = _testOrganizationId,
                Role = "Member",
                JoinedAt = DateTime.UtcNow.AddDays(-3)
            };
            var membership2 = new TeamMembership
            {
                Id = Guid.NewGuid(),
                UserId = member2Id,
                TeamId = _testTeamId,
                OrganizationId = _testOrganizationId,
                Role = "TeamAdmin",
                JoinedAt = DateTime.UtcNow.AddDays(-2)
            };
            var membership3 = new TeamMembership
            {
                Id = Guid.NewGuid(),
                UserId = member3Id,
                TeamId = _testTeamId,
                OrganizationId = _testOrganizationId,
                Role = "Member",
                JoinedAt = DateTime.UtcNow.AddDays(-1)
            };

            _dbContext.Users.AddRange(user1, user2, user3);
            _dbContext.TeamMemberships.AddRange(membership1, membership2, membership3);
            await _dbContext.SaveChangesAsync();

            // Act
            var result = await _controller.ListMembers(_testOrganizationId, _testTeamId, 20, 0, CancellationToken.None);

            // Assert
            result.Should().BeOfType<OkObjectResult>();
            var okResult = result as OkObjectResult;
            okResult!.Value.Should().NotBeNull();

            var response = okResult.Value!;
            var responseType = response.GetType();
            var members = responseType.GetProperty("members")?.GetValue(response) as System.Collections.IList;
            members.Should().NotBeNull();
            members!.Count.Should().Be(4); // 3 new members + 1 admin from SetupTeamAdminPermission

            var total = responseType.GetProperty("total")?.GetValue(response);
            total.Should().Be(4);

            var page = responseType.GetProperty("page")?.GetValue(response);
            page.Should().Be(0);

            var pageSize = responseType.GetProperty("pageSize")?.GetValue(response);
            pageSize.Should().Be(20);
        }

        [Fact]
        public async Task ListMembers_WithPagination_ReturnsCorrectPage()
        {
            // Arrange
            SetupTeamAdminPermission();

            var team = CreateTestTeam();
            _dbContext.Teams.Add(team);
            await _dbContext.SaveChangesAsync();

            // Create 5 members
            for (int i = 1; i <= 5; i++)
            {
                var userId = Guid.NewGuid();
                var user = new User
                {
                    Id = userId,
                    OrganizationId = _testOrganizationId,
                    Email = $"member{i}@example.com",
                    PasswordHash = "hashedpassword",
                    FirstName = "Member",
                    LastName = i.ToString(System.Globalization.CultureInfo.InvariantCulture),
                    Role = "member",
                    DataResidencyRegion = "us-east-1",
                    CreatedInRegion = "us-east-1",
                    IsActive = true,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow,
                };
                var membership = new TeamMembership
                {
                    Id = Guid.NewGuid(),
                    UserId = userId,
                    TeamId = _testTeamId,
                    OrganizationId = _testOrganizationId,
                    Role = "Member",
                    JoinedAt = DateTime.UtcNow
                };
                _dbContext.Users.Add(user);
                _dbContext.TeamMemberships.Add(membership);
            }
            await _dbContext.SaveChangesAsync();

            // Act - Get first page with 2 items
            var result = await _controller.ListMembers(_testOrganizationId, _testTeamId, 2, 0, CancellationToken.None);

            // Assert
            result.Should().BeOfType<OkObjectResult>();
            var okResult = result as OkObjectResult;
            okResult!.Value.Should().NotBeNull();

            var response = okResult.Value!;
            var responseType = response.GetType();
            var members = responseType.GetProperty("members")?.GetValue(response) as System.Collections.IList;
            members.Should().NotBeNull();
            members!.Count.Should().Be(2);

            var total = responseType.GetProperty("total")?.GetValue(response);
            total.Should().Be(6); // 5 new members + 1 admin
        }

        [Fact]
        public async Task ListMembers_WithEmptyTeam_ReturnsEmptyList()
        {
            // Arrange
            SetupTeamAdminPermission();

            var team = CreateTestTeam();
            _dbContext.Teams.Add(team);
            await _dbContext.SaveChangesAsync();

            // Act
            var result = await _controller.ListMembers(_testOrganizationId, _testTeamId, 20, 0, CancellationToken.None);

            // Assert
            result.Should().BeOfType<OkObjectResult>();
            var okResult = result as OkObjectResult;
            okResult!.Value.Should().NotBeNull();

            var response = okResult.Value!;
            var responseType = response.GetType();
            var members = responseType.GetProperty("members")?.GetValue(response) as System.Collections.IList;
            members.Should().NotBeNull();
            members!.Count.Should().Be(1); // Only the admin from SetupTeamAdminPermission

            var total = responseType.GetProperty("total")?.GetValue(response);
            total.Should().Be(1);
        }

        [Fact]
        public async Task ListMembers_ReturnsMemberDetails()
        {
            // Arrange
            SetupTeamAdminPermission();

            var team = CreateTestTeam();
            _dbContext.Teams.Add(team);
            await _dbContext.SaveChangesAsync();

            var memberId = Guid.NewGuid();
            var user = new User
            {
                Id = memberId,
                OrganizationId = _testOrganizationId,
                Email = "test@example.com",
                PasswordHash = "hashedpassword",
                FirstName = "Test",
                LastName = "User",
                Role = "member",
                DataResidencyRegion = "us-east-1",
                CreatedInRegion = "us-east-1",
                IsActive = true,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
            };
            var membership = new TeamMembership
            {
                Id = Guid.NewGuid(),
                UserId = memberId,
                TeamId = _testTeamId,
                OrganizationId = _testOrganizationId,
                Role = "TeamAdmin",
                JoinedAt = DateTime.UtcNow.AddDays(-1),
                InvitedBy = _testUserId
            };

            _dbContext.Users.Add(user);
            _dbContext.TeamMemberships.Add(membership);
            await _dbContext.SaveChangesAsync();

            // Act
            var result = await _controller.ListMembers(_testOrganizationId, _testTeamId, 20, 0, CancellationToken.None);

            // Assert
            result.Should().BeOfType<OkObjectResult>();
            var okResult = result as OkObjectResult;
            okResult!.Value.Should().NotBeNull();

            var response = okResult.Value!;
            var responseType = response.GetType();
            var members = responseType.GetProperty("members")?.GetValue(response) as System.Collections.IList;
            members.Should().NotBeNull();

            // Find the member we added
            var memberItem = members!.Cast<object>().FirstOrDefault(item =>
            {
                var itemType = item.GetType();
                var userId = itemType.GetProperty("userId")?.GetValue(item);
                return userId?.Equals(memberId) == true;
            });

            memberItem.Should().NotBeNull();
            var memberType = memberItem!.GetType();
            memberType.GetProperty("userId")?.GetValue(memberItem).Should().Be(memberId);
            memberType.GetProperty("email")?.GetValue(memberItem).Should().Be("test@example.com");
            memberType.GetProperty("firstName")?.GetValue(memberItem).Should().Be("Test");
            memberType.GetProperty("lastName")?.GetValue(memberItem).Should().Be("User");
            memberType.GetProperty("role")?.GetValue(memberItem).Should().Be("TeamAdmin");
            memberType.GetProperty("joinedAt")?.GetValue(memberItem).Should().NotBeNull();
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
