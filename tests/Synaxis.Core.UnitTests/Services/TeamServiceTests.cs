// <copyright file="TeamServiceTests.cs" company="Synaxis">
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
    using Moq;
    using Synaxis.Core.Contracts;
    using Synaxis.Core.Models;
    using Synaxis.Infrastructure.Data;
    using Synaxis.Infrastructure.Services;
    using Xunit;

    [Trait("Category", "Unit")]
    public class TeamServiceTests : IAsyncLifetime
    {
        private SynaxisDbContext _context;
        private Mock<IInvitationService> _invitationServiceMock;
        private TeamService _service;
        private Guid _organizationId;
        private Guid _userId;

        public async Task InitializeAsync()
        {
            var options = new DbContextOptionsBuilder<SynaxisDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;

            _context = new SynaxisDbContext(options);
            _invitationServiceMock = new Mock<IInvitationService>();
            _service = new TeamService(_context, _invitationServiceMock.Object);

            _organizationId = Guid.NewGuid();
            _userId = Guid.NewGuid();

            var organization = new Organization
            {
                Id = _organizationId,
                Slug = "test-org",
                Name = "Test Organization",
                PrimaryRegion = "eu-west-1",
                MaxTeams = 10,
            };

            _context.Organizations.Add(organization);
            await _context.SaveChangesAsync();
        }

        public async Task DisposeAsync()
        {
            await _context.DisposeAsync();
        }

        [Fact]
        public async Task CreateTeamAsync_ValidRequest_CreatesTeam()
        {
            var request = new CreateTeamRequest
            {
                Slug = "engineering",
                Name = "Engineering Team",
                Description = "Main engineering team",
                MonthlyBudget = 1000.00m,
                AllowedModels = new List<string> { "gpt-4", "claude-3" },
            };

            var result = await _service.CreateTeamAsync(request, _organizationId, _userId);

            result.Should().NotBeNull();
            result.Slug.Should().Be("engineering");
            result.Name.Should().Be("Engineering Team");
            result.Description.Should().Be("Main engineering team");
            result.MonthlyBudget.Should().Be(1000.00m);
            result.OrganizationId.Should().Be(_organizationId);
            result.IsActive.Should().BeTrue();
            result.MemberCount.Should().Be(0);

            var teamInDb = await _context.Teams.FindAsync(result.Id);
            teamInDb.Should().NotBeNull();
        }

        [Fact]
        public async Task CreateTeamAsync_DuplicateSlug_ThrowsInvalidOperationException()
        {
            var existingTeam = new Team
            {
                Id = Guid.NewGuid(),
                OrganizationId = _organizationId,
                Slug = "engineering",
                Name = "Existing Engineering Team",
            };
            _context.Teams.Add(existingTeam);
            await _context.SaveChangesAsync();

            var request = new CreateTeamRequest
            {
                Slug = "engineering",
                Name = "New Engineering Team",
            };

            var act = async () => await _service.CreateTeamAsync(request, _organizationId, _userId);

            await act.Should().ThrowAsync<InvalidOperationException>()
                .WithMessage("*slug*already exists*");
        }

        [Fact]
        public async Task CreateTeamAsync_OrganizationLimitExceeded_ThrowsInvalidOperationException()
        {
            var organization = await _context.Organizations.FindAsync(_organizationId);
            organization!.MaxTeams = 2;
            await _context.SaveChangesAsync();

            for (int i = 0; i < 2; i++)
            {
                _context.Teams.Add(new Team
                {
                    Id = Guid.NewGuid(),
                    OrganizationId = _organizationId,
                    Slug = $"team-{i}",
                    Name = $"Team {i}",
                });
            }
            await _context.SaveChangesAsync();

            var request = new CreateTeamRequest
            {
                Slug = "new-team",
                Name = "New Team",
            };

            var act = async () => await _service.CreateTeamAsync(request, _organizationId, _userId);

            await act.Should().ThrowAsync<InvalidOperationException>()
                .WithMessage("*maximum limit*");
        }

        [Fact]
        public async Task CreateTeamAsync_NullRequest_ThrowsArgumentNullException()
        {
            var act = async () => await _service.CreateTeamAsync(null!, _organizationId, _userId);

            await act.Should().ThrowAsync<ArgumentNullException>();
        }

        [Fact]
        public async Task CreateTeamAsync_EmptySlug_ThrowsArgumentException()
        {
            var request = new CreateTeamRequest
            {
                Slug = "",
                Name = "Test Team",
            };

            var act = async () => await _service.CreateTeamAsync(request, _organizationId, _userId);

            await act.Should().ThrowAsync<ArgumentException>()
                .WithMessage("*slug*");
        }

        [Fact]
        public async Task CreateTeamAsync_EmptyName_ThrowsArgumentException()
        {
            var request = new CreateTeamRequest
            {
                Slug = "test-team",
                Name = "",
            };

            var act = async () => await _service.CreateTeamAsync(request, _organizationId, _userId);

            await act.Should().ThrowAsync<ArgumentException>()
                .WithMessage("*name*");
        }

        [Fact]
        public async Task GetTeamAsync_TeamExists_ReturnsTeam()
        {
            var team = new Team
            {
                Id = Guid.NewGuid(),
                OrganizationId = _organizationId,
                Slug = "data-science",
                Name = "Data Science Team",
                Description = "AI/ML team",
                MonthlyBudget = 2000.00m,
            };
            _context.Teams.Add(team);
            await _context.SaveChangesAsync();

            var result = await _service.GetTeamAsync(team.Id, _organizationId);

            result.Should().NotBeNull();
            result!.Id.Should().Be(team.Id);
            result.Slug.Should().Be("data-science");
            result.Name.Should().Be("Data Science Team");
            result.Description.Should().Be("AI/ML team");
            result.MonthlyBudget.Should().Be(2000.00m);
        }

        [Fact]
        public async Task GetTeamAsync_TeamNotFound_ReturnsNull()
        {
            var result = await _service.GetTeamAsync(Guid.NewGuid(), _organizationId);

            result.Should().BeNull();
        }

        [Fact]
        public async Task GetTeamAsync_WrongTenant_ReturnsNull()
        {
            var team = new Team
            {
                Id = Guid.NewGuid(),
                OrganizationId = _organizationId,
                Slug = "finance",
                Name = "Finance Team",
            };
            _context.Teams.Add(team);
            await _context.SaveChangesAsync();

            var result = await _service.GetTeamAsync(team.Id, Guid.NewGuid());

            result.Should().BeNull();
        }

        [Fact]
        public async Task UpdateTeamAsync_ValidUpdate_UpdatesTeam()
        {
            var team = new Team
            {
                Id = Guid.NewGuid(),
                OrganizationId = _organizationId,
                Slug = "operations",
                Name = "Operations Team",
            };
            _context.Teams.Add(team);
            await _context.SaveChangesAsync();

            var request = new UpdateTeamRequest
            {
                Name = "Updated Operations Team",
                Description = "Updated description",
                MonthlyBudget = 1500.00m,
                AllowedModels = new List<string> { "gpt-4" },
            };

            var result = await _service.UpdateTeamAsync(team.Id, request);

            result.Should().NotBeNull();
            result.Name.Should().Be("Updated Operations Team");
            result.Description.Should().Be("Updated description");
            result.MonthlyBudget.Should().Be(1500.00m);

            var updatedTeam = await _context.Teams.FindAsync(team.Id);
            updatedTeam!.Name.Should().Be("Updated Operations Team");
            updatedTeam.UpdatedAt.Should().BeAfter(DateTime.UtcNow.AddSeconds(-1));
        }

        [Fact]
        public async Task UpdateTeamAsync_TeamNotFound_ThrowsInvalidOperationException()
        {
            var request = new UpdateTeamRequest
            {
                Name = "Updated Name",
            };

            var act = async () => await _service.UpdateTeamAsync(Guid.NewGuid(), request);

            await act.Should().ThrowAsync<InvalidOperationException>()
                .WithMessage("*not found*");
        }

        [Fact]
        public async Task UpdateTeamAsync_NullRequest_ThrowsArgumentNullException()
        {
            var act = async () => await _service.UpdateTeamAsync(Guid.NewGuid(), null!);

            await act.Should().ThrowAsync<ArgumentNullException>();
        }

        [Fact]
        public async Task DeleteTeamAsync_TeamExists_SoftDeletesTeam()
        {
            var team = new Team
            {
                Id = Guid.NewGuid(),
                OrganizationId = _organizationId,
                Slug = "marketing",
                Name = "Marketing Team",
            };
            _context.Teams.Add(team);
            await _context.SaveChangesAsync();

            await _service.DeleteTeamAsync(team.Id, _organizationId);

            var deletedTeam = await _context.Teams.FindAsync(team.Id);
            deletedTeam.Should().NotBeNull();
            deletedTeam!.IsActive.Should().BeFalse();
        }

        [Fact]
        public async Task DeleteTeamAsync_TeamNotFound_ThrowsInvalidOperationException()
        {
            var act = async () => await _service.DeleteTeamAsync(Guid.NewGuid(), _organizationId);

            await act.Should().ThrowAsync<InvalidOperationException>()
                .WithMessage("*not found*");
        }

        [Fact]
        public async Task DeleteTeamAsync_WrongTenant_ThrowsInvalidOperationException()
        {
            var team = new Team
            {
                Id = Guid.NewGuid(),
                OrganizationId = _organizationId,
                Slug = "sales",
                Name = "Sales Team",
            };
            _context.Teams.Add(team);
            await _context.SaveChangesAsync();

            var act = async () => await _service.DeleteTeamAsync(team.Id, Guid.NewGuid());

            await act.Should().ThrowAsync<InvalidOperationException>()
                .WithMessage("*not found*");
        }

        [Fact]
        public async Task ListTeamsAsync_ReturnsAllActiveTeams()
        {
            _context.Teams.AddRange(
                new Team
                {
                    Id = Guid.NewGuid(),
                    OrganizationId = _organizationId,
                    Slug = "team-1",
                    Name = "Team 1",
                    IsActive = true,
                },
                new Team
                {
                    Id = Guid.NewGuid(),
                    OrganizationId = _organizationId,
                    Slug = "team-2",
                    Name = "Team 2",
                    IsActive = true,
                },
                new Team
                {
                    Id = Guid.NewGuid(),
                    OrganizationId = _organizationId,
                    Slug = "team-3",
                    Name = "Team 3",
                    IsActive = false,
                });
            await _context.SaveChangesAsync();

            var result = await _service.ListTeamsAsync(_organizationId, 1, 10);

            result.Should().NotBeNull();
            result.Teams.Should().HaveCount(2);
            result.TotalCount.Should().Be(2);
            result.Page.Should().Be(1);
            result.PageSize.Should().Be(10);
            result.Teams.All(t => t.IsActive).Should().BeTrue();
        }

        [Fact]
        public async Task ListTeamsAsync_EmptyList_ReturnsEmptyList()
        {
            var result = await _service.ListTeamsAsync(_organizationId, 1, 10);

            result.Should().NotBeNull();
            result.Teams.Should().BeEmpty();
            result.TotalCount.Should().Be(0);
        }

        [Fact]
        public async Task ListTeamsAsync_Pagination_WorksCorrectly()
        {
            for (int i = 0; i < 25; i++)
            {
                _context.Teams.Add(new Team
                {
                    Id = Guid.NewGuid(),
                    OrganizationId = _organizationId,
                    Slug = $"team-{i}",
                    Name = $"Team {i}",
                });
            }
            await _context.SaveChangesAsync();

            var page1 = await _service.ListTeamsAsync(_organizationId, 1, 10);
            var page2 = await _service.ListTeamsAsync(_organizationId, 2, 10);
            var page3 = await _service.ListTeamsAsync(_organizationId, 3, 10);

            page1.Teams.Should().HaveCount(10);
            page1.TotalCount.Should().Be(25);
            page2.Teams.Should().HaveCount(10);
            page2.TotalCount.Should().Be(25);
            page3.Teams.Should().HaveCount(5);
            page3.TotalCount.Should().Be(25);

            var allTeamIds = page1.Teams.Concat(page2.Teams).Concat(page3.Teams).Select(t => t.Id).ToList();
            allTeamIds.Should().OnlyHaveUniqueItems();
        }

        [Fact]
        public async Task ListTeamsAsync_FiltersByOrganization()
        {
            var otherOrgId = Guid.NewGuid();
            var otherOrg = new Organization
            {
                Id = otherOrgId,
                Slug = "other-org",
                Name = "Other Organization",
                PrimaryRegion = "us-east-1",
            };
            _context.Organizations.Add(otherOrg);

            _context.Teams.Add(new Team
            {
                Id = Guid.NewGuid(),
                OrganizationId = _organizationId,
                Slug = "my-team",
                Name = "My Team",
            });
            _context.Teams.Add(new Team
            {
                Id = Guid.NewGuid(),
                OrganizationId = otherOrgId,
                Slug = "other-team",
                Name = "Other Team",
            });
            await _context.SaveChangesAsync();

            var result = await _service.ListTeamsAsync(_organizationId, 1, 10);

            result.Teams.Should().HaveCount(1);
            result.Teams.First().Slug.Should().Be("my-team");
        }

        [Fact]
        public async Task InviteMemberAsync_CreatesInvitation()
        {
            var team = new Team
            {
                Id = Guid.NewGuid(),
                OrganizationId = _organizationId,
                Slug = "product",
                Name = "Product Team",
            };
            _context.Teams.Add(team);
            await _context.SaveChangesAsync();

            var email = "newmember@example.com";
            var role = "Member";

            await _service.InviteMemberAsync(team.Id, email, role, _userId);

            _invitationServiceMock.Verify(
                x => x.CreateInvitationAsync(team.Id, email, role, _userId, It.IsAny<CancellationToken>()),
                Times.Once);
        }

        [Fact]
        public async Task InviteMemberAsync_TeamNotFound_ThrowsInvalidOperationException()
        {
            var act = async () => await _service.InviteMemberAsync(Guid.NewGuid(), "test@example.com", "Member", _userId);

            await act.Should().ThrowAsync<InvalidOperationException>()
                .WithMessage("*not found*");
        }

        [Fact]
        public async Task InviteMemberAsync_EmptyEmail_ThrowsArgumentException()
        {
            var team = new Team
            {
                Id = Guid.NewGuid(),
                OrganizationId = _organizationId,
                Slug = "support",
                Name = "Support Team",
            };
            _context.Teams.Add(team);
            await _context.SaveChangesAsync();

            var act = async () => await _service.InviteMemberAsync(team.Id, "", "Member", _userId);

            await act.Should().ThrowAsync<ArgumentException>()
                .WithMessage("*email*");
        }

        [Fact]
        public async Task InviteMemberAsync_EmptyRole_ThrowsArgumentException()
        {
            var team = new Team
            {
                Id = Guid.NewGuid(),
                OrganizationId = _organizationId,
                Slug = "hr",
                Name = "HR Team",
            };
            _context.Teams.Add(team);
            await _context.SaveChangesAsync();

            var act = async () => await _service.InviteMemberAsync(team.Id, "test@example.com", "", _userId);

            await act.Should().ThrowAsync<ArgumentException>()
                .WithMessage("*role*");
        }
    }
}
