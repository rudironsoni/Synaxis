// <copyright file="TeamServiceTests.cs" company="Synaxis">
// Copyright (c) Synaxis. All rights reserved.
// </copyright>

namespace Synaxis.Infrastructure.Tests.Services
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

    /// <summary>
    /// Unit tests for TeamService (RED phase - TDD).
    /// </summary>
    [Trait("Category", "Unit")]
    public sealed class TeamServiceTests : IAsyncLifetime, IDisposable
    {
        private SynaxisDbContext? _context;
        private Mock<IInvitationService> _invitationServiceMock = null!;
        private TeamService? _service;
        private Guid _organizationId;
        private Guid _userId;

        public async Task InitializeAsync()
        {
            var options = new DbContextOptionsBuilder<SynaxisDbContext>()
                .UseInMemoryDatabase(databaseName: $"TeamServiceTests_{Guid.NewGuid()}")
                .Options;

            if (_context is not null)
            {
                await _context.DisposeAsync();
            }

            _context = new SynaxisDbContext(options);
            _invitationServiceMock = new Mock<IInvitationService>();

            // Seed organization and user
            _organizationId = Guid.NewGuid();
            _userId = Guid.NewGuid();

            var organization = new Organization
            {
                Id = _organizationId,
                Slug = "test-org",
                Name = "Test Organization",
                Tier = "starter",
                PrimaryRegion = "us-east-1",
                IsActive = true,
                IsVerified = true
            };

            var user = new User
            {
                Id = _userId,
                OrganizationId = _organizationId,
                Email = "admin@test.com",
                Role = "OrgAdmin",
                DataResidencyRegion = "us-east-1",
                CreatedInRegion = "us-east-1",
                PasswordHash = "dummy-hash-for-testing"
            };

            _context!.Organizations.Add(organization);
            _context!.Users.Add(user);
            await _context!.SaveChangesAsync();

            _service = new TeamService(_context, _invitationServiceMock.Object);
        }

        public async Task DisposeAsync()
        {
            if (_context is not null)
            {
                await _context!.DisposeAsync();
            }
        }

        public void Dispose()
        {
            _context?.Dispose();
        }

        [Fact]
        public async Task CreateTeamAsync_WithValidRequest_CreatesTeam()
        {
            // Arrange
            var request = new CreateTeamRequest
            {
                Slug = "test-team",
                Name = "Test Team",
                Description = "Test Description",
                MonthlyBudget = 100.00m,
                AllowedModels = new List<string> { "gpt-4", "claude-3" }
            };

            // Act
            var result = await _service!.CreateTeamAsync(request, _organizationId, _userId);

            // Assert
            result.Should().NotBeNull();
            result.Slug.Should().Be("test-team");
            result.Name.Should().Be("Test Team");
            result.Description.Should().Be("Test Description");
            result.MonthlyBudget.Should().Be(100.00m);
            result.IsActive.Should().BeTrue();
            result.OrganizationId.Should().Be(_organizationId);

            var dbTeam = await _context!.Teams.FirstOrDefaultAsync(t => t.Slug == "test-team");
            dbTeam.Should().NotBeNull();
        }

        [Fact]
        public async Task CreateTeamAsync_WithDuplicateSlug_ThrowsInvalidOperationException()
        {
            // Arrange
            var existingTeam = new Team
            {
                Id = Guid.NewGuid(),
                OrganizationId = _organizationId,
                Slug = "duplicate-team",
                Name = "Existing Team",
                IsActive = true
            };
            _context!.Teams.Add(existingTeam);
            await _context!.SaveChangesAsync();

            var request = new CreateTeamRequest
            {
                Slug = "duplicate-team",
                Name = "New Team"
            };

            // Act
            Func<Task> act = async () => await _service!.CreateTeamAsync(request, _organizationId, _userId);

            // Assert
            await act.Should().ThrowAsync<InvalidOperationException>()
                .WithMessage("*slug*already exists*");
        }

        [Fact]
        public async Task CreateTeamAsync_WithInvalidOrganization_ThrowsInvalidOperationException()
        {
            // Arrange
            var invalidOrgId = Guid.NewGuid();
            var request = new CreateTeamRequest
            {
                Slug = "test-team",
                Name = "Test Team"
            };

            // Act
            Func<Task> act = async () => await _service!.CreateTeamAsync(request, invalidOrgId, _userId);

            // Assert
            await act.Should().ThrowAsync<InvalidOperationException>()
                .WithMessage("*Organization*not found*");
        }

        [Fact]
        public async Task CreateTeamAsync_WithNullRequest_ThrowsArgumentNullException()
        {
            // Act
            Func<Task> act = async () => await _service!.CreateTeamAsync(null!, _organizationId, _userId);

            // Assert
            await act.Should().ThrowAsync<ArgumentNullException>();
        }

        [Fact]
        public async Task GetTeamAsync_WithValidTeamId_ReturnsTeam()
        {
            // Arrange
            var teamId = Guid.NewGuid();
            var team = new Team
            {
                Id = teamId,
                OrganizationId = _organizationId,
                Slug = "test-team",
                Name = "Test Team",
                Description = "Test Description",
                IsActive = true,
                MonthlyBudget = 100.00m
            };
            _context!.Teams.Add(team);
            await _context!.SaveChangesAsync();

            // Act
            var result = await _service!.GetTeamAsync(teamId, _organizationId);

            // Assert
            result.Should().NotBeNull();
            result.Id.Should().Be(teamId);
            result.Slug.Should().Be("test-team");
            result.Name.Should().Be("Test Team");
        }

        [Fact]
        public async Task GetTeamAsync_WithNonExistentTeam_ReturnsNull()
        {
            // Arrange
            var nonExistentId = Guid.NewGuid();

            // Act
            var result = await _service!.GetTeamAsync(nonExistentId, _organizationId);

            // Assert
            result.Should().BeNull();
        }

        [Fact]
        public async Task GetTeamAsync_WithWrongOrganization_ReturnsNull()
        {
            // Arrange
            var teamId = Guid.NewGuid();
            var wrongOrgId = Guid.NewGuid();
            var team = new Team
            {
                Id = teamId,
                OrganizationId = _organizationId,
                Slug = "test-team",
                Name = "Test Team",
                IsActive = true
            };
            _context!.Teams.Add(team);
            await _context!.SaveChangesAsync();

            // Act
            var result = await _service!.GetTeamAsync(teamId, wrongOrgId);

            // Assert
            result.Should().BeNull();
        }

        [Fact]
        public async Task UpdateTeamAsync_WithValidRequest_UpdatesTeam()
        {
            // Arrange
            var teamId = Guid.NewGuid();
            var team = new Team
            {
                Id = teamId,
                OrganizationId = _organizationId,
                Slug = "test-team",
                Name = "Original Name",
                IsActive = true
            };
            _context!.Teams.Add(team);
            await _context!.SaveChangesAsync();

            var updateRequest = new UpdateTeamRequest
            {
                Name = "Updated Name",
                Description = "Updated Description",
                MonthlyBudget = 200.00m,
                IsActive = false
            };

            // Act
            var result = await _service!.UpdateTeamAsync(teamId, updateRequest);

            // Assert
            result.Should().NotBeNull();
            result.Name.Should().Be("Updated Name");
            result.Description.Should().Be("Updated Description");
            result.MonthlyBudget.Should().Be(200.00m);
            result.IsActive.Should().BeFalse();
        }

        [Fact]
        public async Task UpdateTeamAsync_WithNonExistentTeam_ThrowsInvalidOperationException()
        {
            // Arrange
            var nonExistentId = Guid.NewGuid();
            var updateRequest = new UpdateTeamRequest
            {
                Name = "Updated Name"
            };

            // Act
            Func<Task> act = async () => await _service!.UpdateTeamAsync(nonExistentId, updateRequest);

            // Assert
            await act.Should().ThrowAsync<InvalidOperationException>()
                .WithMessage("*Team*not found*");
        }

        [Fact]
        public async Task UpdateTeamAsync_WithNullRequest_ThrowsArgumentNullException()
        {
            // Arrange
            var teamId = Guid.NewGuid();

            // Act
            Func<Task> act = async () => await _service!.UpdateTeamAsync(teamId, null!);

            // Assert
            await act.Should().ThrowAsync<ArgumentNullException>();
        }

        [Fact]
        public async Task DeleteTeamAsync_WithValidTeam_MarksTeamInactive()
        {
            // Arrange
            var teamId = Guid.NewGuid();
            var team = new Team
            {
                Id = teamId,
                OrganizationId = _organizationId,
                Slug = "test-team",
                Name = "Test Team",
                IsActive = true
            };
            _context!.Teams.Add(team);
            await _context!.SaveChangesAsync();

            // Act
            await _service!.DeleteTeamAsync(teamId, _organizationId);

            // Assert
            var deletedTeam = await _context!.Teams.FindAsync(teamId);
            deletedTeam.Should().NotBeNull();
            deletedTeam.IsActive.Should().BeFalse();
        }

        [Fact]
        public async Task DeleteTeamAsync_WithNonExistentTeam_ThrowsInvalidOperationException()
        {
            // Arrange
            var nonExistentId = Guid.NewGuid();

            // Act
            Func<Task> act = async () => await _service!.DeleteTeamAsync(nonExistentId, _organizationId);

            // Assert
            await act.Should().ThrowAsync<InvalidOperationException>()
                .WithMessage("*Team*not found*");
        }

        [Fact]
        public async Task DeleteTeamAsync_WithWrongOrganization_ThrowsInvalidOperationException()
        {
            // Arrange
            var teamId = Guid.NewGuid();
            var wrongOrgId = Guid.NewGuid();
            var team = new Team
            {
                Id = teamId,
                OrganizationId = _organizationId,
                Slug = "test-team",
                Name = "Test Team",
                IsActive = true
            };
            _context!.Teams.Add(team);
            await _context!.SaveChangesAsync();

            // Act
            Func<Task> act = async () => await _service!.DeleteTeamAsync(teamId, wrongOrgId);

            // Assert
            await act.Should().ThrowAsync<InvalidOperationException>();
        }

        [Fact]
        public async Task ListTeamsAsync_WithMultipleTeams_ReturnsPaginatedList()
        {
            // Arrange
            for (int i = 1; i <= 5; i++)
            {
                _context!.Teams.Add(new Team
                {
                    Id = Guid.NewGuid(),
                    OrganizationId = _organizationId,
                    Slug = $"team-{i}",
                    Name = $"Team {i}",
                    IsActive = true
                });
            }
            await _context!.SaveChangesAsync();

            // Act
            var result = await _service!.ListTeamsAsync(_organizationId, page: 1, pageSize: 3);

            // Assert
            result.Should().NotBeNull();
            result.Teams.Should().HaveCount(3);
            result.TotalCount.Should().Be(5);
            result.Page.Should().Be(1);
            result.PageSize.Should().Be(3);
        }

        [Fact]
        public async Task ListTeamsAsync_WithNoTeams_ReturnsEmptyList()
        {
            // Act
            var result = await _service!.ListTeamsAsync(_organizationId, page: 1, pageSize: 10);

            // Assert
            result.Should().NotBeNull();
            result.Teams.Should().BeEmpty();
            result.TotalCount.Should().Be(0);
        }

        [Fact]
        public async Task ListTeamsAsync_WithSecondPage_ReturnsCorrectTeams()
        {
            // Arrange
            for (int i = 1; i <= 5; i++)
            {
                _context!.Teams.Add(new Team
                {
                    Id = Guid.NewGuid(),
                    OrganizationId = _organizationId,
                    Slug = $"team-{i}",
                    Name = $"Team {i}",
                    IsActive = true
                });
            }
            await _context!.SaveChangesAsync();

            // Act
            var result = await _service!.ListTeamsAsync(_organizationId, page: 2, pageSize: 3);

            // Assert
            result.Should().NotBeNull();
            result.Teams.Should().HaveCount(2);
            result.TotalCount.Should().Be(5);
            result.Page.Should().Be(2);
        }

        [Fact]
        public async Task ArchiveTeamAsync_WithValidTeam_MarksTeamInactive()
        {
            // Arrange
            var teamId = Guid.NewGuid();
            var team = new Team
            {
                Id = teamId,
                OrganizationId = _organizationId,
                Slug = "test-team",
                Name = "Test Team",
                IsActive = true
            };
            _context!.Teams.Add(team);
            await _context!.SaveChangesAsync();

            // Act
            await _service!.ArchiveTeamAsync(teamId, _organizationId);

            // Assert
            var archivedTeam = await _context!.Teams.FindAsync(teamId);
            archivedTeam.Should().NotBeNull();
            archivedTeam.IsActive.Should().BeFalse();
        }

        [Fact]
        public async Task ArchiveTeamAsync_WithNonExistentTeam_ThrowsInvalidOperationException()
        {
            // Arrange
            var nonExistentId = Guid.NewGuid();

            // Act
            Func<Task> act = async () => await _service!.ArchiveTeamAsync(nonExistentId, _organizationId);

            // Assert
            await act.Should().ThrowAsync<InvalidOperationException>()
                .WithMessage("*Team*not found*");
        }

        [Fact]
        public async Task RestoreTeamAsync_WithArchivedTeam_MarksTeamActive()
        {
            // Arrange
            var teamId = Guid.NewGuid();
            var team = new Team
            {
                Id = teamId,
                OrganizationId = _organizationId,
                Slug = "test-team",
                Name = "Test Team",
                IsActive = false
            };
            _context!.Teams.Add(team);
            await _context!.SaveChangesAsync();

            // Act
            await _service!.RestoreTeamAsync(teamId, _organizationId);

            // Assert
            var restoredTeam = await _context!.Teams.FindAsync(teamId);
            restoredTeam.Should().NotBeNull();
            restoredTeam.IsActive.Should().BeTrue();
        }

        [Fact]
        public async Task RestoreTeamAsync_WithNonExistentTeam_ThrowsInvalidOperationException()
        {
            // Arrange
            var nonExistentId = Guid.NewGuid();

            // Act
            Func<Task> act = async () => await _service!.RestoreTeamAsync(nonExistentId, _organizationId);

            // Assert
            await act.Should().ThrowAsync<InvalidOperationException>()
                .WithMessage("*Team*not found*");
        }

        [Fact]
        public async Task ValidateTeamSlugAsync_WithUniqueSlug_ReturnsTrue()
        {
            // Arrange
            var slug = "unique-team";

            // Act
            var result = await _service!.ValidateTeamSlugAsync(slug, _organizationId);

            // Assert
            result.Should().BeTrue();
        }

        [Fact]
        public async Task ValidateTeamSlugAsync_WithDuplicateSlug_ReturnsFalse()
        {
            // Arrange
            var slug = "duplicate-team";
            _context!.Teams.Add(new Team
            {
                Id = Guid.NewGuid(),
                OrganizationId = _organizationId,
                Slug = slug,
                Name = "Existing Team",
                IsActive = true
            });
            await _context!.SaveChangesAsync();

            // Act
            var result = await _service!.ValidateTeamSlugAsync(slug, _organizationId);

            // Assert
            result.Should().BeFalse();
        }

        [Fact]
        public async Task ValidateTeamSlugAsync_WithExcludedTeamId_ReturnsTrue()
        {
            // Arrange
            var teamId = Guid.NewGuid();
            var slug = "existing-team";
            _context!.Teams.Add(new Team
            {
                Id = teamId,
                OrganizationId = _organizationId,
                Slug = slug,
                Name = "Existing Team",
                IsActive = true
            });
            await _context!.SaveChangesAsync();

            // Act
            var result = await _service!.ValidateTeamSlugAsync(slug, _organizationId, teamId);

            // Assert
            result.Should().BeTrue();
        }

        [Fact]
        public async Task ValidateTeamSlugAsync_WithDuplicateInDifferentOrganization_ReturnsTrue()
        {
            // Arrange
            var otherOrgId = Guid.NewGuid();
            var slug = "duplicate-slug";
            _context!.Organizations.Add(new Organization
            {
                Id = otherOrgId,
                Slug = "other-org",
                Name = "Other Org",
                Tier = "starter",
                PrimaryRegion = "us-east-1",
                IsActive = true
            });
            _context!.Teams.Add(new Team
            {
                Id = Guid.NewGuid(),
                OrganizationId = otherOrgId,
                Slug = slug,
                Name = "Other Org Team",
                IsActive = true
            });
            await _context!.SaveChangesAsync();

            // Act
            var result = await _service!.ValidateTeamSlugAsync(slug, _organizationId);

            // Assert
            result.Should().BeTrue();
        }

        private async Task<User> CreateUserAsync(Guid userId, Guid orgId, string email, string firstName, string lastName)
        {
            var user = new User
            {
                Id = userId,
                OrganizationId = orgId,
                Email = email,
                Role = "Member",
                DataResidencyRegion = "us-east-1",
                CreatedInRegion = "us-east-1",
                PasswordHash = "dummy-hash-for-testing",
                FirstName = firstName,
                LastName = lastName,
            };
            _context!.Users.Add(user);
            await _context!.SaveChangesAsync();
            return user;
        }

        [Fact]
        public async Task GetTeamStatsAsync_WithTeamAndMembers_ReturnsCorrectStats()
        {
            // Arrange
            var teamId = Guid.NewGuid();
            var team = new Team
            {
                Id = teamId,
                OrganizationId = _organizationId,
                Slug = "test-team",
                Name = "Test Team",
                IsActive = true,
                MonthlyBudget = 1000.00m,
            };
            _context!.Teams.Add(team);

            // Add members using helper method
            var member1 = await CreateUserAsync(Guid.NewGuid(), _organizationId, "member1@test.com", "Member", "One");
            var member2 = await CreateUserAsync(Guid.NewGuid(), _organizationId, "member2@test.com", "Member", "Two");
            var member3 = await CreateUserAsync(Guid.NewGuid(), _organizationId, "member3@test.com", "Member", "Three");

            _context!.TeamMemberships.Add(new TeamMembership { Id = Guid.NewGuid(), UserId = member1.Id, TeamId = teamId, OrganizationId = _organizationId, Role = "Member", JoinedAt = DateTime.UtcNow });
            _context!.TeamMemberships.Add(new TeamMembership { Id = Guid.NewGuid(), UserId = member2.Id, TeamId = teamId, OrganizationId = _organizationId, Role = "Member", JoinedAt = DateTime.UtcNow });
            _context!.TeamMemberships.Add(new TeamMembership { Id = Guid.NewGuid(), UserId = member3.Id, TeamId = teamId, OrganizationId = _organizationId, Role = "Member", JoinedAt = DateTime.UtcNow });

            // Add virtual keys
            for (int i = 1; i <= 2; i++)
            {
                _context!.VirtualKeys.Add(new VirtualKey
                {
                    Id = Guid.NewGuid(),
                    OrganizationId = _organizationId,
                    TeamId = teamId,
                    CreatedBy = _userId,
                    Name = $"Key {i}",
                    KeyHash = $"hash-{i}",
                    IsActive = true,
                    IsRevoked = false,
                    UserRegion = "us-east-1",
                });
            }

            await _context!.SaveChangesAsync();

            // Act
            var result = await _service!.GetTeamStatsAsync(teamId, _organizationId);

            // Assert
            result.Should().NotBeNull();
            result.TeamId.Should().Be(teamId);
            result.MemberCount.Should().Be(3);
            result.ActiveKeyCount.Should().Be(2);
            result.MonthlyBudget.Should().Be(1000.00m);
        }

        [Fact]
        public async Task GetTeamStatsAsync_WithEmptyTeam_ReturnsZeroStats()
        {
            // Arrange
            var teamId = Guid.NewGuid();
            var team = new Team
            {
                Id = teamId,
                OrganizationId = _organizationId,
                Slug = "empty-team",
                Name = "Empty Team",
                IsActive = true
            };
            _context!.Teams.Add(team);
            await _context!.SaveChangesAsync();

            // Act
            var result = await _service!.GetTeamStatsAsync(teamId, _organizationId);

            // Assert
            result.Should().NotBeNull();
            result.MemberCount.Should().Be(0);
            result.ActiveKeyCount.Should().Be(0);
        }

        [Fact]
        public async Task GetTeamStatsAsync_WithNonExistentTeam_ThrowsInvalidOperationException()
        {
            // Arrange
            var nonExistentId = Guid.NewGuid();

            // Act
            Func<Task> act = async () => await _service!.GetTeamStatsAsync(nonExistentId, _organizationId);

            // Assert
            await act.Should().ThrowAsync<InvalidOperationException>()
                .WithMessage("*Team*not found*");
        }

        [Fact]
        public async Task InviteMemberAsync_WithValidEmail_CallsInvitationService()
        {
            // Arrange
            var teamId = Guid.NewGuid();
            var team = new Team
            {
                Id = teamId,
                OrganizationId = _organizationId,
                Slug = "test-team",
                Name = "Test Team",
                IsActive = true
            };
            _context!.Teams.Add(team);
            await _context!.SaveChangesAsync();

            var email = "newmember@test.com";
            var role = "Member";

            // Act
            await _service!.InviteMemberAsync(teamId, email, role, _userId);

            // Assert
            _invitationServiceMock.Verify(
                x => x.CreateInvitationAsync(teamId, email, role, _userId, It.IsAny<CancellationToken>()),
                Times.Once);
        }

        [Fact]
        public async Task InviteMemberAsync_WithInvalidTeam_ThrowsInvalidOperationException()
        {
            // Arrange
            var nonExistentTeamId = Guid.NewGuid();

            // Act
            Func<Task> act = async () => await _service!.InviteMemberAsync(
                nonExistentTeamId, "test@test.com", "Member", _userId);

            // Assert
            await act.Should().ThrowAsync<InvalidOperationException>()
                .WithMessage("*Team*not found*");
        }
    }
}
