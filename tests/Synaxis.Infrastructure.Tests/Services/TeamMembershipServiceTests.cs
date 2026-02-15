// <copyright file="TeamMembershipServiceTests.cs" company="Synaxis">
// Copyright (c) Synaxis. All rights reserved.
// </copyright>

namespace Synaxis.Infrastructure.Tests.Services
{
    using FluentAssertions;
    using Microsoft.EntityFrameworkCore;
    using Synaxis.Core.Contracts;
    using Synaxis.Core.Models;
    using Synaxis.Infrastructure.Data;
    using Synaxis.Infrastructure.Services;
    using Xunit;

    [Trait("Category", "Unit")]
    public sealed class TeamMembershipServiceTests : IAsyncLifetime, IDisposable
    {
        private SynaxisDbContext? _context;
        private ITeamMembershipService? _service;
        private readonly Guid _orgId = Guid.NewGuid();
        private readonly Guid _teamId = Guid.NewGuid();
        private readonly Guid _userId = Guid.NewGuid();
        private readonly Guid _adminUserId = Guid.NewGuid();

        public async Task InitializeAsync()
        {
            var options = new DbContextOptionsBuilder<SynaxisDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;

            if (_context is not null)
            {
                await _context.DisposeAsync();
            }

            _context = new SynaxisDbContext(options);
            _service = new TeamMembershipService(_context);

            await Task.CompletedTask;
        }

        public async Task DisposeAsync()
        {
            if (_context is not null)
            {
                await _context.DisposeAsync();
            }
        }

        public void Dispose()
        {
            _context?.Dispose();
        }

        [Fact]
        public async Task AddMemberAsync_ValidRequest_ReturnsMemberResponse()
        {
            var user = new User
            {
                Id = _userId,
                OrganizationId = _orgId,
                Email = "user@test.com",
                FirstName = "Test",
                LastName = "User",
                PasswordHash = "hash",
                DataResidencyRegion = "us",
                CreatedInRegion = "us"
            };

            var team = new Team
            {
                Id = _teamId,
                OrganizationId = _orgId,
                Slug = "test-team",
                Name = "Test Team"
            };

            _context!.Users.Add(user);
            _context.Teams.Add(team);
            await _context.SaveChangesAsync();

            var result = await _service!.AddMemberAsync(_teamId, _userId, "Member", _adminUserId);

            result.Should().NotBeNull();
            result.UserId.Should().Be(_userId);
            result.TeamId.Should().Be(_teamId);
            result.Role.Should().Be("Member");
            result.UserEmail.Should().Be("user@test.com");
            result.UserFullName.Should().Be("Test User");
        }

        [Fact]
        public async Task AddMemberAsync_UserAlreadyMember_ThrowsInvalidOperationException()
        {
            var user = new User
            {
                Id = _userId,
                OrganizationId = _orgId,
                Email = "user@test.com",
                PasswordHash = "hash",
                DataResidencyRegion = "us",
                CreatedInRegion = "us"
            };

            var team = new Team
            {
                Id = _teamId,
                OrganizationId = _orgId,
                Slug = "test-team",
                Name = "Test Team"
            };

            var existingMembership = new TeamMembership
            {
                Id = Guid.NewGuid(),
                UserId = _userId,
                TeamId = _teamId,
                OrganizationId = _orgId,
                Role = "Member"
            };

            _context!.Users.Add(user);
            _context.Teams.Add(team);
            _context.TeamMemberships.Add(existingMembership);
            await _context.SaveChangesAsync();

            var act = async () => await _service!.AddMemberAsync(_teamId, _userId, "Member", _adminUserId);

            await act.Should().ThrowAsync<InvalidOperationException>()
                .WithMessage("*already a member*");
        }

        [Fact]
        public async Task AddMemberAsync_InvalidRole_ThrowsArgumentException()
        {
            var user = new User
            {
                Id = _userId,
                OrganizationId = _orgId,
                Email = "user@test.com",
                PasswordHash = "hash",
                DataResidencyRegion = "us",
                CreatedInRegion = "us"
            };

            var team = new Team
            {
                Id = _teamId,
                OrganizationId = _orgId,
                Slug = "test-team",
                Name = "Test Team"
            };

            _context!.Users.Add(user);
            _context.Teams.Add(team);
            await _context.SaveChangesAsync();

            var act = async () => await _service!.AddMemberAsync(_teamId, _userId, "invalid_role", _adminUserId);

            await act.Should().ThrowAsync<ArgumentException>()
                .WithMessage("*Invalid role*");
        }

        [Fact]
        public async Task AddMemberAsync_UserNotInOrganization_ThrowsInvalidOperationException()
        {
            var user = new User
            {
                Id = _userId,
                OrganizationId = Guid.NewGuid(),
                Email = "user@test.com",
                PasswordHash = "hash",
                DataResidencyRegion = "us",
                CreatedInRegion = "us"
            };

            var team = new Team
            {
                Id = _teamId,
                OrganizationId = _orgId,
                Slug = "test-team",
                Name = "Test Team"
            };

            _context!.Users.Add(user);
            _context.Teams.Add(team);
            await _context.SaveChangesAsync();

            var act = async () => await _service!.AddMemberAsync(_teamId, _userId, "Member", _adminUserId);

            await act.Should().ThrowAsync<InvalidOperationException>()
                .WithMessage("*not in the same organization*");
        }

        [Fact]
        public async Task RemoveMemberAsync_ValidRequest_RemovesMember()
        {
            var membership = new TeamMembership
            {
                Id = Guid.NewGuid(),
                UserId = _userId,
                TeamId = _teamId,
                OrganizationId = _orgId,
                Role = "Member"
            };

            _context!.TeamMemberships.Add(membership);
            await _context.SaveChangesAsync();

            await _service!.RemoveMemberAsync(_teamId, _userId, _adminUserId);

            var removed = await _context.TeamMemberships
                .FirstOrDefaultAsync(m => m.UserId == _userId && m.TeamId == _teamId);
            removed.Should().BeNull();
        }

        [Fact]
        public async Task RemoveMemberAsync_UserNotFound_ThrowsInvalidOperationException()
        {
            var act = async () => await _service!.RemoveMemberAsync(_teamId, _userId, _adminUserId);

            await act.Should().ThrowAsync<InvalidOperationException>()
                .WithMessage("*not found*");
        }

        [Fact]
        public async Task UpdateMemberRoleAsync_ValidRoleChange_UpdatesRole()
        {
            var user = new User
            {
                Id = _userId,
                OrganizationId = _orgId,
                Email = "user@test.com",
                FirstName = "Test",
                LastName = "User",
                PasswordHash = "hash",
                DataResidencyRegion = "us",
                CreatedInRegion = "us"
            };

            var membership = new TeamMembership
            {
                Id = Guid.NewGuid(),
                UserId = _userId,
                TeamId = _teamId,
                OrganizationId = _orgId,
                Role = "Member"
            };

            _context!.Users.Add(user);
            _context.TeamMemberships.Add(membership);
            await _context.SaveChangesAsync();

            var result = await _service!.UpdateMemberRoleAsync(_teamId, _userId, "TeamAdmin", _adminUserId);

            result.Should().NotBeNull();
            result.Role.Should().Be("TeamAdmin");

            var updated = await _context!.TeamMemberships
                .FirstOrDefaultAsync(m => m.UserId == _userId && m.TeamId == _teamId);
            updated.Should().NotBeNull();
            updated!.Role.Should().Be("TeamAdmin");
        }

        [Fact]
        public async Task UpdateMemberRoleAsync_InvalidRole_ThrowsArgumentException()
        {
            var membership = new TeamMembership
            {
                Id = Guid.NewGuid(),
                UserId = _userId,
                TeamId = _teamId,
                OrganizationId = _orgId,
                Role = "Member"
            };

            _context!.TeamMemberships.Add(membership);
            await _context.SaveChangesAsync();

            var act = async () => await _service!.UpdateMemberRoleAsync(_teamId, _userId, "invalid_role", _adminUserId);

            await act.Should().ThrowAsync<ArgumentException>()
                .WithMessage("*Invalid role*");
        }

        [Fact]
        public async Task UpdateMemberRoleAsync_UserNotFound_ThrowsInvalidOperationException()
        {
            var act = async () => await _service!.UpdateMemberRoleAsync(_teamId, _userId, "TeamAdmin", _adminUserId);

            await act.Should().ThrowAsync<InvalidOperationException>()
                .WithMessage("*not found*");
        }

        [Fact]
        public async Task GetTeamMembersAsync_WithPagination_ReturnsPagedResults()
        {
            var user1 = new User { Id = Guid.NewGuid(), Email = "user1@test.com", OrganizationId = _orgId, PasswordHash = "hash", DataResidencyRegion = "us", CreatedInRegion = "us", FirstName = "User", LastName = "One" };
            var user2 = new User { Id = Guid.NewGuid(), Email = "user2@test.com", OrganizationId = _orgId, PasswordHash = "hash", DataResidencyRegion = "us", CreatedInRegion = "us", FirstName = "User", LastName = "Two" };

            var memberships = new List<TeamMembership>
            {
                new TeamMembership { Id = Guid.NewGuid(), UserId = user1.Id, TeamId = _teamId, OrganizationId = _orgId, Role = "TeamAdmin" },
                new TeamMembership { Id = Guid.NewGuid(), UserId = user2.Id, TeamId = _teamId, OrganizationId = _orgId, Role = "Member" }
            };

            _context!.Users.AddRange(user1, user2);
            _context.TeamMemberships.AddRange(memberships);
            await _context.SaveChangesAsync();

            var result = await _service!.GetTeamMembersAsync(_teamId, 1, 10);

            result.Should().NotBeNull();
            result.Members.Should().HaveCount(2);
            result.TotalCount.Should().Be(2);
            result.Page.Should().Be(1);
            result.PageSize.Should().Be(10);
        }

        [Fact]
        public async Task GetTeamMembersAsync_EmptyTeam_ReturnsEmptyList()
        {
            var result = await _service!.GetTeamMembersAsync(_teamId, 1, 10);

            result.Should().NotBeNull();
            result.Members.Should().BeEmpty();
            result.TotalCount.Should().Be(0);
        }

        [Fact]
        public async Task GetUserTeamsAsync_MultipleTeams_ReturnsAllTeams()
        {
            var org = new Organization { Id = _orgId, Name = "Test Org", Slug = "test-org", PrimaryRegion = "us" };
            var team1 = new Team { Id = Guid.NewGuid(), OrganizationId = _orgId, Name = "Team 1", Slug = "team-1" };
            var team2 = new Team { Id = Guid.NewGuid(), OrganizationId = _orgId, Name = "Team 2", Slug = "team-2" };

            var memberships = new List<TeamMembership>
            {
                new TeamMembership { Id = Guid.NewGuid(), UserId = _userId, TeamId = team1.Id, OrganizationId = _orgId, Role = "TeamAdmin" },
                new TeamMembership { Id = Guid.NewGuid(), UserId = _userId, TeamId = team2.Id, OrganizationId = _orgId, Role = "Member" }
            };

            _context!.Organizations.Add(org);
            _context.Teams.AddRange(team1, team2);
            _context.TeamMemberships.AddRange(memberships);
            await _context.SaveChangesAsync();

            var result = await _service!.GetUserTeamsAsync(_userId, 1, 10);

            result.Should().NotBeNull();
            result.Members.Should().HaveCount(2);
            result.TotalCount.Should().Be(2);
        }

        [Fact]
        public async Task GetUserTeamsAsync_WithPagination_ReturnsCorrectPage()
        {
            var org = new Organization { Id = _orgId, Name = "Test Org", Slug = "test-org", PrimaryRegion = "us" };
            var team1 = new Team { Id = Guid.NewGuid(), OrganizationId = _orgId, Name = "Team 1", Slug = "team-1" };
            var team2 = new Team { Id = Guid.NewGuid(), OrganizationId = _orgId, Name = "Team 2", Slug = "team-2" };

            var memberships = new List<TeamMembership>
            {
                new TeamMembership { Id = Guid.NewGuid(), UserId = _userId, TeamId = team1.Id, OrganizationId = _orgId, Role = "TeamAdmin" },
                new TeamMembership { Id = Guid.NewGuid(), UserId = _userId, TeamId = team2.Id, OrganizationId = _orgId, Role = "Member" }
            };

            _context!.Organizations.Add(org);
            _context.Teams.AddRange(team1, team2);
            _context.TeamMemberships.AddRange(memberships);
            await _context.SaveChangesAsync();

            var result = await _service!.GetUserTeamsAsync(_userId, 1, 1);

            result.Should().NotBeNull();
            result.Members.Should().HaveCount(1);
            result.TotalCount.Should().Be(2);
            result.PageSize.Should().Be(1);
        }

        [Fact]
        public async Task GetUserTeamsAsync_EmptyResult_ReturnsEmptyList()
        {
            var result = await _service!.GetUserTeamsAsync(_userId, 1, 10);

            result.Should().NotBeNull();
            result.Members.Should().BeEmpty();
            result.TotalCount.Should().Be(0);
        }

        [Fact]
        public async Task CheckPermissionAsync_AdminHasPermission_ReturnsTrue()
        {
            var membership = new TeamMembership
            {
                Id = Guid.NewGuid(),
                UserId = _userId,
                TeamId = _teamId,
                OrganizationId = _orgId,
                Role = "TeamAdmin"
            };

            _context!.TeamMemberships.Add(membership);
            await _context.SaveChangesAsync();

            var result = await _service!.CheckPermissionAsync(_userId, _teamId, "manage_team");

            result.Should().BeTrue();
        }

        [Fact]
        public async Task CheckPermissionAsync_MemberLacksAdminPermission_ReturnsFalse()
        {
            var membership = new TeamMembership
            {
                Id = Guid.NewGuid(),
                UserId = _userId,
                TeamId = _teamId,
                OrganizationId = _orgId,
                Role = "Member"
            };

            _context!.TeamMemberships.Add(membership);
            await _context.SaveChangesAsync();

            var result = await _service!.CheckPermissionAsync(_userId, _teamId, "manage_team");

            result.Should().BeFalse();
        }

        [Fact]
        public async Task CheckPermissionAsync_UserNotMember_ReturnsFalse()
        {
            var result = await _service!.CheckPermissionAsync(_userId, _teamId, "manage_team");

            result.Should().BeFalse();
        }
    }
}
