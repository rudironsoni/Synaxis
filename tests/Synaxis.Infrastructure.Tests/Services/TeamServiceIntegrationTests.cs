// <copyright file="TeamServiceIntegrationTests.cs" company="Synaxis">
// Copyright (c) Synaxis. All rights reserved.
// </copyright>

namespace Synaxis.Infrastructure.Tests.Services
{
    using System;
    using System.Linq;
    using System.Threading.Tasks;
    using FluentAssertions;
    using Microsoft.EntityFrameworkCore;
    using Moq;
    using Synaxis.Core.Contracts;
    using Synaxis.Core.Models;
    using Synaxis.Infrastructure.Data;
    using Synaxis.Infrastructure.Services;
    using Testcontainers.PostgreSql;
    using Xunit;

    [Trait("Category", "Integration")]
    public sealed class TeamServiceIntegrationTests : IAsyncLifetime, IDisposable
    {
        private readonly PostgreSqlContainer _postgresContainer = new PostgreSqlBuilder("postgres:16-alpine")
            .WithDatabase("synaxis_test")
            .WithUsername("postgres")
            .WithPassword("testpassword")
            .Build();

        private SynaxisDbContext? _context;
        private Mock<IInvitationService> _invitationServiceMock = null!;
        private TeamService? _service;
        private Guid _organizationId;
        private Guid _userId;

        public async Task InitializeAsync()
        {
            await _postgresContainer.StartAsync();

            var options = new DbContextOptionsBuilder<SynaxisDbContext>()
                .UseNpgsql(_postgresContainer.GetConnectionString())
                .ConfigureWarnings(warnings =>
                    warnings.Ignore(Microsoft.EntityFrameworkCore.Diagnostics.RelationalEventId.PendingModelChangesWarning))
                .Options;

            if (_context is not null)
            {
                await _context.DisposeAsync();
            }

            _context = new SynaxisDbContext(options);
            await _context!.Database.MigrateAsync();

            _invitationServiceMock = new Mock<IInvitationService>();
            _service = new TeamService(_context, _invitationServiceMock.Object);

            _organizationId = Guid.NewGuid();
            _userId = Guid.NewGuid();

            var organization = new Organization
            {
                Id = _organizationId,
                Slug = "integration-test-org",
                Name = "Integration Test Organization",
                PrimaryRegion = "eu-west-1",
                MaxTeams = 10,
            };

            _context!.Organizations.Add(organization);
            await _context!.SaveChangesAsync();
        }

        public async Task DisposeAsync()
        {
            if (_context is not null)
            {
                await _context!.DisposeAsync();
            }

            await _postgresContainer.DisposeAsync();
        }

        public void Dispose()
        {
            _context?.Dispose();
            _postgresContainer.DisposeAsync().AsTask().Wait();
        }

        [Fact]
        public async Task CreateTeamAsync_FullLifecycle_WorksCorrectly()
        {
            var request = new CreateTeamRequest
            {
                Slug = "engineering-team",
                Name = "Engineering Team",
                Description = "Main engineering team",
                MonthlyBudget = 5000.00m,
            };

            var createdTeam = await _service!.CreateTeamAsync(request, _organizationId, _userId);

            createdTeam.Should().NotBeNull();
            createdTeam.Id.Should().NotBeEmpty();
            createdTeam.Slug.Should().Be("engineering-team");

            var retrievedTeam = await _service!.GetTeamAsync(createdTeam.Id, _organizationId);
            retrievedTeam.Should().NotBeNull();
            retrievedTeam!.Name.Should().Be("Engineering Team");

            var updateRequest = new UpdateTeamRequest
            {
                Name = "Updated Engineering Team",
                MonthlyBudget = 7500.00m,
            };

            var updatedTeam = await _service!.UpdateTeamAsync(createdTeam.Id, updateRequest);
            updatedTeam.Name.Should().Be("Updated Engineering Team");
            updatedTeam.MonthlyBudget.Should().Be(7500.00m);

            await _service!.DeleteTeamAsync(createdTeam.Id, _organizationId);

            var deletedTeam = await _context!.Teams.FindAsync(createdTeam.Id);
            deletedTeam.Should().NotBeNull();
            deletedTeam!.IsActive.Should().BeFalse();
        }

        [Fact]
        public async Task TenantIsolation_MultipleOrganizations_AreIsolated()
        {
            var org2Id = Guid.NewGuid();
            var org2 = new Organization
            {
                Id = org2Id,
                Slug = "test-org-2",
                Name = "Test Organization 2",
                PrimaryRegion = "us-east-1",
                MaxTeams = 10,
            };
            _context!.Organizations.Add(org2);
            await _context!.SaveChangesAsync();

            var team1 = await _service!.CreateTeamAsync(
                new CreateTeamRequest { Slug = "team1", Name = "Team 1" },
                _organizationId,
                _userId);

            var team2 = await _service!.CreateTeamAsync(
                new CreateTeamRequest { Slug = "team2", Name = "Team 2" },
                org2Id,
                _userId);

            var org1Teams = await _service!.ListTeamsAsync(_organizationId, 1, 10);
            org1Teams.Teams.Should().HaveCount(1);
            org1Teams.Teams[0].Id.Should().Be(team1.Id);

            var org2Teams = await _service!.ListTeamsAsync(org2Id, 1, 10);
            org2Teams.Teams.Should().HaveCount(1);
            org2Teams.Teams[0].Id.Should().Be(team2.Id);

            var team1FromOrg2 = await _service!.GetTeamAsync(team1.Id, org2Id);
            team1FromOrg2.Should().BeNull();
        }

        [Fact]
        public async Task UniqueConstraint_DuplicateSlugWithinOrg_ThrowsException()
        {
            await _service!.CreateTeamAsync(
                new CreateTeamRequest { Slug = "duplicate-test", Name = "Team 1" },
                _organizationId,
                _userId);

            var act = async () => await _service!.CreateTeamAsync(
                new CreateTeamRequest { Slug = "duplicate-test", Name = "Team 2" },
                _organizationId,
                _userId);

            await act.Should().ThrowAsync<InvalidOperationException>()
                .WithMessage("*already exists*");
        }

        [Fact]
        public async Task UniqueConstraint_SameSlugDifferentOrgs_Allowed()
        {
            var org2Id = Guid.NewGuid();
            var org2 = new Organization
            {
                Id = org2Id,
                Slug = "test-org-3",
                Name = "Test Organization 3",
                PrimaryRegion = "us-east-1",
                MaxTeams = 10,
            };
            _context!.Organizations.Add(org2);
            await _context!.SaveChangesAsync();

            var team1 = await _service!.CreateTeamAsync(
                new CreateTeamRequest { Slug = "shared-slug", Name = "Team 1" },
                _organizationId,
                _userId);

            var team2 = await _service!.CreateTeamAsync(
                new CreateTeamRequest { Slug = "shared-slug", Name = "Team 2" },
                org2Id,
                _userId);

            team1.Should().NotBeNull();
            team2.Should().NotBeNull();
            team1.Id.Should().NotBe(team2.Id);
        }

        [Fact]
        public async Task Pagination_LargeDataset_WorksCorrectly()
        {
            // Increase team limit for this test
            var organization = await _context!.Organizations.FindAsync(_organizationId);
            organization!.MaxTeams = 50;
            await _context!.SaveChangesAsync();

            for (int i = 0; i < 25; i++)
            {
                await _service!.CreateTeamAsync(
                    new CreateTeamRequest
                    {
                        Slug = $"team-{i:D3}",
                        Name = $"Team {i}",
                    },
                    _organizationId,
                    _userId);
            }

            var page1 = await _service!.ListTeamsAsync(_organizationId, 1, 10);
            var page2 = await _service!.ListTeamsAsync(_organizationId, 2, 10);
            var page3 = await _service!.ListTeamsAsync(_organizationId, 3, 10);

            page1.Teams.Should().HaveCount(10);
            page2.Teams.Should().HaveCount(10);
            page3.Teams.Should().HaveCount(5);
            page1.TotalCount.Should().Be(25);

            var allIds = page1.Teams.Concat(page2.Teams).Concat(page3.Teams).Select(t => t.Id).ToList();
            allIds.Should().OnlyHaveUniqueItems();
        }

        [Fact]
        public async Task SequentialModifications_SameTeam_HandledCorrectly()
        {
            var team = await _service!.CreateTeamAsync(
                new CreateTeamRequest { Slug = "concurrent-test", Name = "Concurrent Test Team" },
                _organizationId,
                _userId);

            // Sequential updates (DbContext is not thread-safe for concurrent operations)
            await _service!.UpdateTeamAsync(team.Id, new UpdateTeamRequest { Name = "Update 1" });
            await _service!.UpdateTeamAsync(team.Id, new UpdateTeamRequest { Name = "Update 2" });

            var finalTeam = await _service!.GetTeamAsync(team.Id, _organizationId);
            finalTeam.Should().NotBeNull();
            finalTeam!.Name.Should().Be("Update 2");
        }

        [Fact]
        public async Task OrganizationLimit_EnforcedCorrectly()
        {
            var organization = await _context!.Organizations.FindAsync(_organizationId);
            organization!.MaxTeams = 2;
            await _context!.SaveChangesAsync();

            await _service!.CreateTeamAsync(
                new CreateTeamRequest { Slug = "limit-test-1", Name = "Team 1" },
                _organizationId,
                _userId);

            await _service!.CreateTeamAsync(
                new CreateTeamRequest { Slug = "limit-test-2", Name = "Team 2" },
                _organizationId,
                _userId);

            var act = async () => await _service!.CreateTeamAsync(
                new CreateTeamRequest { Slug = "limit-test-3", Name = "Team 3" },
                _organizationId,
                _userId);

            await act.Should().ThrowAsync<InvalidOperationException>()
                .WithMessage("*maximum limit*");
        }

        [Fact]
        public async Task SoftDelete_DoesNotDeleteFromDatabase()
        {
            var team = await _service!.CreateTeamAsync(
                new CreateTeamRequest { Slug = "soft-delete-test", Name = "Soft Delete Test" },
                _organizationId,
                _userId);

            await _service!.DeleteTeamAsync(team.Id, _organizationId);

            var teamInDb = await _context!.Teams.FindAsync(team.Id);
            teamInDb.Should().NotBeNull();
            teamInDb!.IsActive.Should().BeFalse();

            var retrievedTeam = await _service!.GetTeamAsync(team.Id, _organizationId);
            retrievedTeam.Should().BeNull();
        }

        [Fact]
        public async Task ListTeams_ExcludesSoftDeletedTeams()
        {
            var team1 = await _service!.CreateTeamAsync(
                new CreateTeamRequest { Slug = "list-test-1", Name = "Team 1" },
                _organizationId,
                _userId);

            var team2 = await _service!.CreateTeamAsync(
                new CreateTeamRequest { Slug = "list-test-2", Name = "Team 2" },
                _organizationId,
                _userId);

            await _service!.DeleteTeamAsync(team1.Id, _organizationId);

            var teams = await _service!.ListTeamsAsync(_organizationId, 1, 10);
            teams.TotalCount.Should().Be(1);
            teams.Teams.Should().HaveCount(1);
            teams.Teams[0].Id.Should().Be(team2.Id);
        }

        [Fact]
        public async Task DatabaseConstraints_EnforcedCorrectly()
        {
            var team = new Team
            {
                Id = Guid.NewGuid(),
                OrganizationId = _organizationId,
                Slug = "constraint-test",
                Name = "Constraint Test",
                MonthlyBudget = -100m,
            };

            _context!.Teams.Add(team);

            var act = async () => await _context!.SaveChangesAsync();

            await act.Should().ThrowAsync<DbUpdateException>();
        }

        [Fact]
        public async Task InviteMember_DelegatesToInvitationService()
        {
            var team = await _service!.CreateTeamAsync(
                new CreateTeamRequest { Slug = "invite-test", Name = "Invite Test Team" },
                _organizationId,
                _userId);

            var email = "newmember@example.com";
            var role = "Member";

            await _service!.InviteMemberAsync(team.Id, email, role, _userId);

            _invitationServiceMock.Verify(
                x => x.CreateInvitationAsync(team.Id, email, role, _userId, default),
                Times.Once);
        }
    }
}
