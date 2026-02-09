// <copyright file="InvitationServiceTests.cs" company="Synaxis">
// Copyright (c) Synaxis. All rights reserved.
// </copyright>

namespace Synaxis.Core.Services.Tests;

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
public class InvitationServiceTests
{
    private readonly SynaxisDbContext _dbContext;
    private readonly Mock<ITeamMembershipService> _teamMembershipServiceMock;
    private readonly InvitationService _service;
    private readonly Guid _organizationId = Guid.NewGuid();
    private readonly Guid _teamId = Guid.NewGuid();
    private readonly Guid _inviterId = Guid.NewGuid();
    private readonly Guid _userId = Guid.NewGuid();

    public InvitationServiceTests()
    {
        var dbOptions = new DbContextOptionsBuilder<SynaxisDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        this._dbContext = new SynaxisDbContext(dbOptions);
        this._teamMembershipServiceMock = new Mock<ITeamMembershipService>();

        this._service = new InvitationService(this._dbContext, this._teamMembershipServiceMock.Object);
    }

    [Fact]
    public async Task CreateInvitationAsync_Success_ReturnsInvitationResponse()
    {
        // Arrange
        var email = "test@example.com";
        var role = "member";
        var team = new Team
        {
            Id = this._teamId,
            OrganizationId = this._organizationId,
            Name = "Test Team",
            Organization = new Organization { Id = this._organizationId, Name = "Test Org" },
        };

        this._dbContext.Organizations.Add(team.Organization);
        this._dbContext.Teams.Add(team);
        await this._dbContext.SaveChangesAsync();

        // Act
        var result = await this._service.CreateInvitationAsync(this._teamId, email, role, this._inviterId);

        // Assert
        result.Should().NotBeNull();
        result.Email.Should().Be(email);
        result.Role.Should().Be(role);
        result.TeamId.Should().Be(this._teamId);
        result.Status.Should().Be("pending");
        result.Token.Should().NotBeNullOrEmpty();
        result.Token!.Length.Should().BeGreaterThanOrEqualTo(43); // Base64 URL-safe 32 bytes minimum
        result.ExpiresAt.Should().BeCloseTo(DateTime.UtcNow.AddDays(7), TimeSpan.FromMinutes(1));
        this._dbContext.Invitations.Should().ContainSingle();
    }

    [Fact]
    public async Task CreateInvitationAsync_UserAlreadyMember_ThrowsInvalidOperationException()
    {
        // Arrange
        var email = "test@example.com";
        var role = "member";
        var org = new Organization { Id = this._organizationId, Name = "Test Org", Slug = "test-org" };
        var team = new Team
        {
            Id = this._teamId,
            OrganizationId = this._organizationId,
            Name = "Test Team",
            Slug = "test-team",
            Organization = org,
        };
        var user = new User { Id = this._userId, Email = email, OrganizationId = this._organizationId };
        var membership = new TeamMembership { UserId = this._userId, TeamId = this._teamId };

        this._dbContext.Organizations.Add(org);
        this._dbContext.Teams.Add(team);
        this._dbContext.Users.Add(user);
        this._dbContext.TeamMemberships.Add(membership);
        await this._dbContext.SaveChangesAsync();

        // Act
        var act = async () => await this._service.CreateInvitationAsync(this._teamId, email, role, this._inviterId);

        // Assert
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*already a member*");
    }

    [Fact]
    public async Task CreateInvitationAsync_InvalidEmail_ThrowsArgumentException()
    {
        // Arrange
        var invalidEmail = "not-an-email";
        var role = "member";
        var org = new Organization { Id = this._organizationId, Name = "Test Org", Slug = "test-org" };
        var team = new Team
        {
            Id = this._teamId,
            OrganizationId = this._organizationId,
            Name = "Test Team",
            Slug = "test-team",
            Organization = org,
        };

        this._dbContext.Organizations.Add(org);
        this._dbContext.Teams.Add(team);
        await this._dbContext.SaveChangesAsync();

        // Act
        var act = async () => await this._service.CreateInvitationAsync(this._teamId, invalidEmail, role, this._inviterId);

        // Assert
        await act.Should().ThrowAsync<ArgumentException>()
            .WithMessage("*provided email address is invalid*");
    }

    [Fact]
    public async Task CreateInvitationAsync_DuplicatePending_ThrowsInvalidOperationException()
    {
        // Arrange
        var email = "test@example.com";
        var role = "member";
        var org = new Organization { Id = this._organizationId, Name = "Test Org", Slug = "test-org" };
        var team = new Team
        {
            Id = this._teamId,
            OrganizationId = this._organizationId,
            Name = "Test Team",
            Slug = "test-team",
            Organization = org,
        };
        var existingInvitation = new Invitation
        {
            Id = Guid.NewGuid(),
            TeamId = this._teamId,
            OrganizationId = this._organizationId,
            Email = email,
            Role = "member",
            Token = "existing-token",
            Status = "pending",
            InvitedBy = this._inviterId,
            CreatedAt = DateTime.UtcNow,
            ExpiresAt = DateTime.UtcNow.AddDays(7),
        };

        this._dbContext.Organizations.Add(org);
        this._dbContext.Teams.Add(team);
        this._dbContext.Invitations.Add(existingInvitation);
        await this._dbContext.SaveChangesAsync();

        // Act
        var act = async () => await this._service.CreateInvitationAsync(this._teamId, email, role, this._inviterId);

        // Assert
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*pending invitation*");
    }

    [Fact]
    public async Task GetInvitationAsync_ValidToken_ReturnsInvitation()
    {
        // Arrange
        var token = "valid-token-12345";
        var org = new Organization { Id = this._organizationId, Name = "Test Org", Slug = "test-org" };
        var team = new Team
        {
            Id = this._teamId,
            OrganizationId = this._organizationId,
            Name = "Test Team",
            Slug = "test-team",
            Organization = org,
        };
        var invitation = new Invitation
        {
            Id = Guid.NewGuid(),
            TeamId = this._teamId,
            OrganizationId = this._organizationId,
            Email = "test@example.com",
            Role = "member",
            Token = token,
            Status = "pending",
            InvitedBy = this._inviterId,
            CreatedAt = DateTime.UtcNow,
            ExpiresAt = DateTime.UtcNow.AddDays(7),
        };

        this._dbContext.Organizations.Add(org);
        this._dbContext.Teams.Add(team);
        this._dbContext.Invitations.Add(invitation);
        await this._dbContext.SaveChangesAsync();

        // Act
        var result = await this._service.GetInvitationAsync(token);

        // Assert
        result.Should().NotBeNull();
        result!.Token.Should().Be(token);
        result.Email.Should().Be(invitation.Email);
        result.Status.Should().Be("pending");
    }

    [Fact]
    public async Task GetInvitationAsync_ExpiredToken_ReturnsInvitationWithExpiredFlag()
    {
        // Arrange
        var token = "expired-token-12345";
        var org = new Organization { Id = this._organizationId, Name = "Test Org", Slug = "test-org" };
        var team = new Team
        {
            Id = this._teamId,
            OrganizationId = this._organizationId,
            Name = "Test Team",
            Slug = "test-team",
            Organization = org,
        };
        var invitation = new Invitation
        {
            Id = Guid.NewGuid(),
            TeamId = this._teamId,
            OrganizationId = this._organizationId,
            Email = "test@example.com",
            Role = "member",
            Token = token,
            Status = "pending",
            InvitedBy = this._inviterId,
            CreatedAt = DateTime.UtcNow.AddDays(-10),
            ExpiresAt = DateTime.UtcNow.AddDays(-3),
        };

        this._dbContext.Organizations.Add(org);
        this._dbContext.Teams.Add(team);
        this._dbContext.Invitations.Add(invitation);
        await this._dbContext.SaveChangesAsync();

        // Act
        var result = await this._service.GetInvitationAsync(token);

        // Assert
        result.Should().NotBeNull();
        result!.IsExpired.Should().BeTrue();
    }

    [Fact]
    public async Task GetInvitationAsync_NotFound_ReturnsNull()
    {
        // Arrange
        var token = "nonexistent-token";

        // Act
        var result = await this._service.GetInvitationAsync(token);

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task GetInvitationAsync_InvalidToken_ReturnsNull()
    {
        // Arrange
        var token = string.Empty;

        // Act
        var result = await this._service.GetInvitationAsync(token);

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task AcceptInvitationAsync_Success_AddsUserToTeam()
    {
        // Arrange
        var token = "valid-token-12345";
        var org = new Organization { Id = this._organizationId, Name = "Test Org", Slug = "test-org" };
        var team = new Team
        {
            Id = this._teamId,
            OrganizationId = this._organizationId,
            Name = "Test Team",
            Slug = "test-team",
            Organization = org,
        };
        var user = new User { Id = this._userId, Email = "test@example.com", OrganizationId = this._organizationId };
        var invitation = new Invitation
        {
            Id = Guid.NewGuid(),
            TeamId = this._teamId,
            OrganizationId = this._organizationId,
            Email = user.Email,
            Role = "member",
            Token = token,
            Status = "pending",
            InvitedBy = this._inviterId,
            CreatedAt = DateTime.UtcNow,
            ExpiresAt = DateTime.UtcNow.AddDays(7),
        };

        this._dbContext.Organizations.Add(org);
        this._dbContext.Teams.Add(team);
        this._dbContext.Users.Add(user);
        this._dbContext.Invitations.Add(invitation);
        await this._dbContext.SaveChangesAsync();

        // Act
        await this._service.AcceptInvitationAsync(token, this._userId);

        // Assert
        var updatedInvitation = await this._dbContext.Invitations.FindAsync(invitation.Id);
        updatedInvitation.Should().NotBeNull();
        updatedInvitation!.Status.Should().Be("accepted");
        updatedInvitation.AcceptedAt.Should().NotBeNull();
        updatedInvitation.AcceptedBy.Should().Be(this._userId);
        this._teamMembershipServiceMock.Verify(x => x.AddMemberAsync(
            this._teamId,
            this._userId,
            invitation.Role,
            invitation.InvitedBy,
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task AcceptInvitationAsync_ExpiredInvitation_ThrowsInvalidOperationException()
    {
        // Arrange
        var token = "expired-token-12345";
        var org = new Organization { Id = this._organizationId, Name = "Test Org", Slug = "test-org" };
        var team = new Team
        {
            Id = this._teamId,
            OrganizationId = this._organizationId,
            Name = "Test Team",
            Slug = "test-team",
            Organization = org,
        };
        var invitation = new Invitation
        {
            Id = Guid.NewGuid(),
            TeamId = this._teamId,
            OrganizationId = this._organizationId,
            Email = "test@example.com",
            Role = "member",
            Token = token,
            Status = "pending",
            InvitedBy = this._inviterId,
            CreatedAt = DateTime.UtcNow.AddDays(-10),
            ExpiresAt = DateTime.UtcNow.AddDays(-3),
        };

        this._dbContext.Organizations.Add(org);
        this._dbContext.Teams.Add(team);
        this._dbContext.Invitations.Add(invitation);
        await this._dbContext.SaveChangesAsync();

        // Act
        var act = async () => await this._service.AcceptInvitationAsync(token, this._userId);

        // Assert
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*expired*");
    }

    [Fact]
    public async Task AcceptInvitationAsync_AlreadyAccepted_ThrowsInvalidOperationException()
    {
        // Arrange
        var token = "accepted-token-12345";
        var org = new Organization { Id = this._organizationId, Name = "Test Org", Slug = "test-org" };
        var team = new Team
        {
            Id = this._teamId,
            OrganizationId = this._organizationId,
            Name = "Test Team",
            Slug = "test-team",
            Organization = org,
        };
        var invitation = new Invitation
        {
            Id = Guid.NewGuid(),
            TeamId = this._teamId,
            OrganizationId = this._organizationId,
            Email = "test@example.com",
            Role = "member",
            Token = token,
            Status = "accepted",
            InvitedBy = this._inviterId,
            CreatedAt = DateTime.UtcNow,
            ExpiresAt = DateTime.UtcNow.AddDays(7),
            AcceptedAt = DateTime.UtcNow,
            AcceptedBy = Guid.NewGuid(),
        };

        this._dbContext.Organizations.Add(org);
        this._dbContext.Teams.Add(team);
        this._dbContext.Invitations.Add(invitation);
        await this._dbContext.SaveChangesAsync();

        // Act
        var act = async () => await this._service.AcceptInvitationAsync(token, this._userId);

        // Assert
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*already been*");
    }

    [Fact]
    public async Task AcceptInvitationAsync_WrongUserEmail_ThrowsInvalidOperationException()
    {
        // Arrange
        var token = "valid-token-12345";
        var org = new Organization { Id = this._organizationId, Name = "Test Org", Slug = "test-org" };
        var team = new Team
        {
            Id = this._teamId,
            OrganizationId = this._organizationId,
            Name = "Test Team",
            Slug = "test-team",
            Organization = org,
        };
        var user = new User { Id = this._userId, Email = "different@example.com", OrganizationId = this._organizationId };
        var invitation = new Invitation
        {
            Id = Guid.NewGuid(),
            TeamId = this._teamId,
            OrganizationId = this._organizationId,
            Email = "test@example.com",
            Role = "member",
            Token = token,
            Status = "pending",
            InvitedBy = this._inviterId,
            CreatedAt = DateTime.UtcNow,
            ExpiresAt = DateTime.UtcNow.AddDays(7),
        };

        this._dbContext.Organizations.Add(org);
        this._dbContext.Teams.Add(team);
        this._dbContext.Users.Add(user);
        this._dbContext.Invitations.Add(invitation);
        await this._dbContext.SaveChangesAsync();

        // Act
        var act = async () => await this._service.AcceptInvitationAsync(token, this._userId);

        // Assert
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*email address*");
    }

    [Fact]
    public async Task DeclineInvitationAsync_Success_MarksInvitationDeclined()
    {
        // Arrange
        var token = "valid-token-12345";
        var org = new Organization { Id = this._organizationId, Name = "Test Org", Slug = "test-org" };
        var team = new Team
        {
            Id = this._teamId,
            OrganizationId = this._organizationId,
            Name = "Test Team",
            Slug = "test-team",
            Organization = org,
        };
        var user = new User { Id = this._userId, Email = "test@example.com", OrganizationId = this._organizationId };
        var invitation = new Invitation
        {
            Id = Guid.NewGuid(),
            TeamId = this._teamId,
            OrganizationId = this._organizationId,
            Email = user.Email,
            Role = "member",
            Token = token,
            Status = "pending",
            InvitedBy = this._inviterId,
            CreatedAt = DateTime.UtcNow,
            ExpiresAt = DateTime.UtcNow.AddDays(7),
        };

        this._dbContext.Organizations.Add(org);
        this._dbContext.Teams.Add(team);
        this._dbContext.Users.Add(user);
        this._dbContext.Invitations.Add(invitation);
        await this._dbContext.SaveChangesAsync();

        // Act
        await this._service.DeclineInvitationAsync(token, this._userId);

        // Assert
        var updatedInvitation = await this._dbContext.Invitations.FindAsync(invitation.Id);
        updatedInvitation.Should().NotBeNull();
        updatedInvitation!.Status.Should().Be("declined");
        updatedInvitation.DeclinedAt.Should().NotBeNull();
        updatedInvitation.DeclinedBy.Should().Be(this._userId);
    }

    [Fact]
    public async Task DeclineInvitationAsync_AlreadyDeclined_ThrowsInvalidOperationException()
    {
        // Arrange
        var token = "declined-token-12345";
        var org = new Organization { Id = this._organizationId, Name = "Test Org", Slug = "test-org" };
        var team = new Team
        {
            Id = this._teamId,
            OrganizationId = this._organizationId,
            Name = "Test Team",
            Slug = "test-team",
            Organization = org,
        };
        var invitation = new Invitation
        {
            Id = Guid.NewGuid(),
            TeamId = this._teamId,
            OrganizationId = this._organizationId,
            Email = "test@example.com",
            Role = "member",
            Token = token,
            Status = "declined",
            InvitedBy = this._inviterId,
            CreatedAt = DateTime.UtcNow,
            ExpiresAt = DateTime.UtcNow.AddDays(7),
            DeclinedAt = DateTime.UtcNow,
            DeclinedBy = Guid.NewGuid(),
        };

        this._dbContext.Organizations.Add(org);
        this._dbContext.Teams.Add(team);
        this._dbContext.Invitations.Add(invitation);
        await this._dbContext.SaveChangesAsync();

        // Act
        var act = async () => await this._service.DeclineInvitationAsync(token, this._userId);

        // Assert
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*already been*");
    }

    [Fact]
    public async Task DeclineInvitationAsync_ExpiredInvitation_ThrowsInvalidOperationException()
    {
        // Arrange
        var token = "expired-token-12345";
        var org = new Organization { Id = this._organizationId, Name = "Test Org", Slug = "test-org" };
        var team = new Team
        {
            Id = this._teamId,
            OrganizationId = this._organizationId,
            Name = "Test Team",
            Slug = "test-team",
            Organization = org,
        };
        var invitation = new Invitation
        {
            Id = Guid.NewGuid(),
            TeamId = this._teamId,
            OrganizationId = this._organizationId,
            Email = "test@example.com",
            Role = "member",
            Token = token,
            Status = "pending",
            InvitedBy = this._inviterId,
            CreatedAt = DateTime.UtcNow.AddDays(-10),
            ExpiresAt = DateTime.UtcNow.AddDays(-3),
        };

        this._dbContext.Organizations.Add(org);
        this._dbContext.Teams.Add(team);
        this._dbContext.Invitations.Add(invitation);
        await this._dbContext.SaveChangesAsync();

        // Act
        var act = async () => await this._service.DeclineInvitationAsync(token, this._userId);

        // Assert
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*expired*");
    }

    [Fact]
    public async Task ListPendingInvitationsAsync_Success_ReturnsPaginatedInvitations()
    {
        // Arrange
        var org = new Organization { Id = this._organizationId, Name = "Test Org", Slug = "test-org" };
        var team = new Team
        {
            Id = this._teamId,
            OrganizationId = this._organizationId,
            Name = "Test Team",
            Slug = "test-team",
            Organization = org,
        };
        var invitation1 = new Invitation
        {
            Id = Guid.NewGuid(),
            TeamId = this._teamId,
            OrganizationId = this._organizationId,
            Email = "test1@example.com",
            Role = "member",
            Token = "token1",
            Status = "pending",
            InvitedBy = this._inviterId,
            CreatedAt = DateTime.UtcNow,
            ExpiresAt = DateTime.UtcNow.AddDays(7),
        };
        var invitation2 = new Invitation
        {
            Id = Guid.NewGuid(),
            TeamId = this._teamId,
            OrganizationId = this._organizationId,
            Email = "test2@example.com",
            Role = "member",
            Token = "token2",
            Status = "pending",
            InvitedBy = this._inviterId,
            CreatedAt = DateTime.UtcNow,
            ExpiresAt = DateTime.UtcNow.AddDays(7),
        };

        this._dbContext.Organizations.Add(org);
        this._dbContext.Teams.Add(team);
        this._dbContext.Invitations.AddRange(invitation1, invitation2);
        await this._dbContext.SaveChangesAsync();

        // Act
        var result = await this._service.ListPendingInvitationsAsync(this._organizationId, 1, 10);

        // Assert
        result.Should().NotBeNull();
        result.Invitations.Should().HaveCount(2);
        result.TotalCount.Should().Be(2);
        result.Page.Should().Be(1);
        result.PageSize.Should().Be(10);
    }

    [Fact]
    public async Task ListPendingInvitationsAsync_OnlyPendingStatus_FiltersCorrectly()
    {
        // Arrange
        var org = new Organization { Id = this._organizationId, Name = "Test Org", Slug = "test-org" };
        var team = new Team
        {
            Id = this._teamId,
            OrganizationId = this._organizationId,
            Name = "Test Team",
            Slug = "test-team",
            Organization = org,
        };
        var pendingInvitation = new Invitation
        {
            Id = Guid.NewGuid(),
            TeamId = this._teamId,
            OrganizationId = this._organizationId,
            Email = "test1@example.com",
            Role = "member",
            Token = "token1",
            Status = "pending",
            InvitedBy = this._inviterId,
            CreatedAt = DateTime.UtcNow,
            ExpiresAt = DateTime.UtcNow.AddDays(7),
        };
        var acceptedInvitation = new Invitation
        {
            Id = Guid.NewGuid(),
            TeamId = this._teamId,
            OrganizationId = this._organizationId,
            Email = "test2@example.com",
            Role = "member",
            Token = "token2",
            Status = "accepted",
            InvitedBy = this._inviterId,
            CreatedAt = DateTime.UtcNow,
            ExpiresAt = DateTime.UtcNow.AddDays(7),
            AcceptedAt = DateTime.UtcNow,
            AcceptedBy = Guid.NewGuid(),
        };

        this._dbContext.Organizations.Add(org);
        this._dbContext.Teams.Add(team);
        this._dbContext.Invitations.AddRange(pendingInvitation, acceptedInvitation);
        await this._dbContext.SaveChangesAsync();

        // Act
        var result = await this._service.ListPendingInvitationsAsync(this._organizationId, 1, 10);

        // Assert
        result.Should().NotBeNull();
        result.Invitations.Should().HaveCount(1);
        result.Invitations.Should().OnlyContain(i => i.Status == "pending");
    }

    [Fact]
    public async Task CancelInvitationAsync_Success_MarksInvitationCancelled()
    {
        // Arrange
        var invitationId = Guid.NewGuid();
        var org = new Organization { Id = this._organizationId, Name = "Test Org", Slug = "test-org" };
        var team = new Team
        {
            Id = this._teamId,
            OrganizationId = this._organizationId,
            Name = "Test Team",
            Slug = "test-team",
            Organization = org,
        };
        var invitation = new Invitation
        {
            Id = invitationId,
            TeamId = this._teamId,
            OrganizationId = this._organizationId,
            Email = "test@example.com",
            Role = "member",
            Token = "token",
            Status = "pending",
            InvitedBy = this._inviterId,
            CreatedAt = DateTime.UtcNow,
            ExpiresAt = DateTime.UtcNow.AddDays(7),
        };

        this._dbContext.Organizations.Add(org);
        this._dbContext.Teams.Add(team);
        this._dbContext.Invitations.Add(invitation);
        await this._dbContext.SaveChangesAsync();

        // Act
        await this._service.CancelInvitationAsync(invitationId, this._inviterId);

        // Assert
        var updatedInvitation = await this._dbContext.Invitations.FindAsync(invitationId);
        updatedInvitation.Should().NotBeNull();
        updatedInvitation!.Status.Should().Be("cancelled");
        updatedInvitation.CancelledAt.Should().NotBeNull();
        updatedInvitation.CancelledBy.Should().Be(this._inviterId);
    }

    [Fact]
    public async Task CancelInvitationAsync_NotFound_ThrowsInvalidOperationException()
    {
        // Arrange
        var invitationId = Guid.NewGuid();

        // Act
        var act = async () => await this._service.CancelInvitationAsync(invitationId, this._inviterId);

        // Assert
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*not found*");
    }

    [Fact]
    public async Task CancelInvitationAsync_AlreadyProcessed_ThrowsInvalidOperationException()
    {
        // Arrange
        var invitationId = Guid.NewGuid();
        var org = new Organization { Id = this._organizationId, Name = "Test Org", Slug = "test-org" };
        var team = new Team
        {
            Id = this._teamId,
            OrganizationId = this._organizationId,
            Name = "Test Team",
            Slug = "test-team",
            Organization = org,
        };
        var invitation = new Invitation
        {
            Id = invitationId,
            TeamId = this._teamId,
            OrganizationId = this._organizationId,
            Email = "test@example.com",
            Role = "member",
            Token = "token",
            Status = "accepted",
            InvitedBy = this._inviterId,
            CreatedAt = DateTime.UtcNow,
            ExpiresAt = DateTime.UtcNow.AddDays(7),
            AcceptedAt = DateTime.UtcNow,
            AcceptedBy = Guid.NewGuid(),
        };

        this._dbContext.Organizations.Add(org);
        this._dbContext.Teams.Add(team);
        this._dbContext.Invitations.Add(invitation);
        await this._dbContext.SaveChangesAsync();

        // Act
        var act = async () => await this._service.CancelInvitationAsync(invitationId, this._inviterId);

        // Assert
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*already been*");
    }
}
