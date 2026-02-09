// <copyright file="TeamMembershipServiceIntegrationTests.cs" company="Synaxis">
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
    [Trait("Category", "Integration")]
    public class TeamMembershipServiceIntegrationTests : IAsyncLifetime
    {
        private readonly PostgreSqlContainer _postgres = new PostgreSqlBuilder()
            .WithImage("postgres:16-alpine")
            .WithDatabase("synaxis_test")
            .WithUsername("test")
            .WithPassword("test")
            .Build();

        private SynaxisDbContext _context = null!;
        private ITeamMembershipService _service = null!;
        private Organization _org = null!;
        private Team _team = null!;
        private User _user1 = null!;
        private User _user2 = null!;
        private Guid _adminUserId;

        public async Task InitializeAsync()
        {
            await _postgres.StartAsync();

            var options = new DbContextOptionsBuilder<SynaxisDbContext>()
                .UseNpgsql(_postgres.GetConnectionString())
                .Options;

            _context = new SynaxisDbContext(options);
            await _context.Database.EnsureCreatedAsync();

            _service = new TeamMembershipService(_context);

            _adminUserId = Guid.NewGuid();
            _org = new Organization
            {
                Id = Guid.NewGuid(),
                Slug = "test-org",
                Name = "Test Organization",
                PrimaryRegion = "us-east-1"
            };

            _team = new Team
            {
                Id = Guid.NewGuid(),
                OrganizationId = _org.Id,
                Slug = "test-team",
                Name = "Test Team"
            };

            _user1 = new User
            {
                Id = Guid.NewGuid(),
                OrganizationId = _org.Id,
                Email = "user1@test.com",
                FirstName = "User",
                LastName = "One",
                PasswordHash = "hash1",
                DataResidencyRegion = "us-east-1",
                CreatedInRegion = "us-east-1"
            };

            _user2 = new User
            {
                Id = Guid.NewGuid(),
                OrganizationId = _org.Id,
                Email = "user2@test.com",
                FirstName = "User",
                LastName = "Two",
                PasswordHash = "hash2",
                DataResidencyRegion = "us-east-1",
                CreatedInRegion = "us-east-1"
            };

            _context.Organizations.Add(_org);
            _context.Teams.Add(_team);
            _context.Users.AddRange(_user1, _user2);
            await _context.SaveChangesAsync();
        }

        public async Task DisposeAsync()
        {
            await _context.DisposeAsync();
            await _postgres.DisposeAsync();
        }

        [Fact]
        public async Task FullMembershipLifecycle_AddUpdateRemove_Success()
        {
            var added = await _service.AddMemberAsync(_team.Id, _user1.Id, "Member", _adminUserId);
            added.Should().NotBeNull();
            added.Role.Should().Be("Member");

            var updated = await _service.UpdateMemberRoleAsync(_team.Id, _user1.Id, "TeamAdmin", _adminUserId);
            updated.Should().NotBeNull();
            updated.Role.Should().Be("TeamAdmin");

            await _service.RemoveMemberAsync(_team.Id, _user1.Id, _adminUserId);

            var membership = await _context.TeamMemberships
                .FirstOrDefaultAsync(m => m.UserId == _user1.Id && m.TeamId == _team.Id);
            membership.Should().BeNull();
        }

        [Fact]
        public async Task ForeignKeyConstraints_DeleteUser_CascadesDelete()
        {
            await _service.AddMemberAsync(_team.Id, _user1.Id, "Member", _adminUserId);

            _context.Users.Remove(_user1);
            await _context.SaveChangesAsync();

            var membership = await _context.TeamMemberships
                .FirstOrDefaultAsync(m => m.UserId == _user1.Id && m.TeamId == _team.Id);
            membership.Should().BeNull();
        }

        [Fact]
        public async Task ForeignKeyConstraints_DeleteTeam_CascadesDelete()
        {
            await _service.AddMemberAsync(_team.Id, _user1.Id, "Member", _adminUserId);

            _context.Teams.Remove(_team);
            await _context.SaveChangesAsync();

            var membership = await _context.TeamMemberships
                .FirstOrDefaultAsync(m => m.UserId == _user1.Id && m.TeamId == _team.Id);
            membership.Should().BeNull();
        }

        [Fact]
        public async Task RoleBasedQueries_FilterByRole_ReturnsCorrectMembers()
        {
            await _service.AddMemberAsync(_team.Id, _user1.Id, "TeamAdmin", _adminUserId);
            await _service.AddMemberAsync(_team.Id, _user2.Id, "Member", _adminUserId);

            var admins = await _context.TeamMemberships
                .Where(m => m.TeamId == _team.Id && m.Role == "TeamAdmin")
                .ToListAsync();

            admins.Should().HaveCount(1);
            admins[0].UserId.Should().Be(_user1.Id);

            var members = await _context.TeamMemberships
                .Where(m => m.TeamId == _team.Id && m.Role == "Member")
                .ToListAsync();

            members.Should().HaveCount(1);
            members[0].UserId.Should().Be(_user2.Id);
        }

        [Fact]
        public async Task ConcurrentMembershipOperations_MultipleUsers_AllSucceed()
        {
            var user3 = new User
            {
                Id = Guid.NewGuid(),
                OrganizationId = _org.Id,
                Email = "user3@test.com",
                PasswordHash = "hash3",
                DataResidencyRegion = "us-east-1",
                CreatedInRegion = "us-east-1"
            };

            _context.Users.Add(user3);
            await _context.SaveChangesAsync();

            await _service.AddMemberAsync(_team.Id, _user1.Id, "Member", _adminUserId);
            await _service.AddMemberAsync(_team.Id, _user2.Id, "TeamAdmin", _adminUserId);
            await _service.AddMemberAsync(_team.Id, user3.Id, "Viewer", _adminUserId);

            var memberships = await _context.TeamMemberships
                .Where(m => m.TeamId == _team.Id)
                .ToListAsync();

            memberships.Should().HaveCount(3);
            memberships.Should().Contain(m => m.UserId == _user1.Id && m.Role == "Member");
            memberships.Should().Contain(m => m.UserId == _user2.Id && m.Role == "TeamAdmin");
            memberships.Should().Contain(m => m.UserId == user3.Id && m.Role == "Viewer");
        }

        [Fact]
        public async Task GetTeamMembers_WithPagination_WorksWithRealDatabase()
        {
            await _service.AddMemberAsync(_team.Id, _user1.Id, "TeamAdmin", _adminUserId);
            await _service.AddMemberAsync(_team.Id, _user2.Id, "Member", _adminUserId);

            var page1 = await _service.GetTeamMembersAsync(_team.Id, 1, 1);
            page1.Should().NotBeNull();
            page1.Members.Should().HaveCount(1);
            page1.TotalCount.Should().Be(2);

            var page2 = await _service.GetTeamMembersAsync(_team.Id, 2, 1);
            page2.Should().NotBeNull();
            page2.Members.Should().HaveCount(1);
            page2.TotalCount.Should().Be(2);

            page1.Members[0].UserId.Should().NotBe(page2.Members[0].UserId);
        }

        [Fact]
        public async Task GetUserTeams_WithMultipleTeams_ReturnsAllTeams()
        {
            var team2 = new Team
            {
                Id = Guid.NewGuid(),
                OrganizationId = _org.Id,
                Slug = "team-2",
                Name = "Team 2"
            };

            _context.Teams.Add(team2);
            await _context.SaveChangesAsync();

            await _service.AddMemberAsync(_team.Id, _user1.Id, "Member", _adminUserId);
            await _service.AddMemberAsync(team2.Id, _user1.Id, "TeamAdmin", _adminUserId);

            var userTeams = await _service.GetUserTeamsAsync(_user1.Id, 1, 10);

            userTeams.Should().NotBeNull();
            userTeams.Members.Should().HaveCount(2);
            userTeams.TotalCount.Should().Be(2);
        }

        [Fact]
        public async Task CheckPermission_WithRealDatabase_ReturnsCorrectResult()
        {
            await _service.AddMemberAsync(_team.Id, _user1.Id, "TeamAdmin", _adminUserId);
            await _service.AddMemberAsync(_team.Id, _user2.Id, "Member", _adminUserId);

            var adminHasPermission = await _service.CheckPermissionAsync(_user1.Id, _team.Id, "manage_team");
            adminHasPermission.Should().BeTrue();

            var memberLacksPermission = await _service.CheckPermissionAsync(_user2.Id, _team.Id, "manage_team");
            memberLacksPermission.Should().BeFalse();

            var memberCanViewTeam = await _service.CheckPermissionAsync(_user2.Id, _team.Id, "view_team");
            memberCanViewTeam.Should().BeTrue();
        }

        [Fact]
        public async Task UniqueConstraint_PreventsDuplicateMemberships()
        {
            await _service.AddMemberAsync(_team.Id, _user1.Id, "Member", _adminUserId);

            var act = async () => await _service.AddMemberAsync(_team.Id, _user1.Id, "TeamAdmin", _adminUserId);

            await act.Should().ThrowAsync<InvalidOperationException>()
                .WithMessage("*already a member*");
        }
    }
}
