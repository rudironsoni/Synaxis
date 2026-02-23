// <copyright file="TeamTests.cs" company="Synaxis">
// Copyright (c) Synaxis. All rights reserved.
// </copyright>

namespace Synaxis.Identity.UnitTests.Aggregates;

using Synaxis.Common.Tests.Time;
using Synaxis.Identity.Domain.Aggregates;
using Synaxis.Identity.Domain.ValueObjects;
using Xunit;
using FluentAssertions;

[Trait("Category", "Unit")]
public class TeamTests
{
    [Fact]
    public void Create_ValidData_CreatesTeam()
    {
        // Arrange
        var id = Guid.NewGuid().ToString();
        var name = TeamName.Create("Test Team");
        var description = "A test team";
        var tenantId = Guid.NewGuid().ToString();
        var timeProvider = new TestTimeProvider();

        // Act
        var team = Team.Create(id, name, description, tenantId, timeProvider);

        // Assert
        team.Id.Should().Be(id);
        team.Name.Should().Be(name);
        team.Description.Should().Be(description);
        team.TenantId.Should().Be(tenantId);
        team.CreatedAt.Should().Be(timeProvider.UtcNow);
        team.UpdatedAt.Should().Be(timeProvider.UtcNow);
        team.Members.Should().BeEmpty();
    }

    [Fact]
    public void AddMember_AddsUserToMembers()
    {
        // Arrange
        var team = CreateTestTeam();
        var userId = Guid.NewGuid().ToString();
        var role = Role.Member;

        // Act
        team.AddMember(userId, role);

        // Assert
        team.Members.Should().HaveCount(1);
        team.Members.Should().ContainKey(userId);
        team.Members[userId].Should().Be(role);
        team.UpdatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(1));
    }

    [Fact]
    public void AddMember_AlreadyMember_ThrowsException()
    {
        // Arrange
        var team = CreateTestTeam();
        var userId = Guid.NewGuid().ToString();
        team.AddMember(userId, Role.Member);

        // Act
        var act = () => team.AddMember(userId, Role.Admin);

        // Assert
        act.Should().Throw<InvalidOperationException>()
            .WithMessage($"User {userId} is already a member of the team.");
    }

    [Fact]
    public void RemoveMember_RemovesUserFromMembers()
    {
        // Arrange
        var team = CreateTestTeam();
        var userId = Guid.NewGuid().ToString();
        team.AddMember(userId, Role.Member);

        // Act
        team.RemoveMember(userId);

        // Assert
        team.Members.Should().BeEmpty();
        team.UpdatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(1));
    }

    [Fact]
    public void RemoveMember_NotMember_ThrowsException()
    {
        // Arrange
        var team = CreateTestTeam();
        var userId = Guid.NewGuid().ToString();

        // Act
        var act = () => team.RemoveMember(userId);

        // Assert
        act.Should().Throw<InvalidOperationException>()
            .WithMessage($"User {userId} is not a member of the team.");
    }

    [Fact]
    public void UpdateMemberRole_UpdatesRole()
    {
        // Arrange
        var team = CreateTestTeam();
        var userId = Guid.NewGuid().ToString();
        team.AddMember(userId, Role.Member);

        // Act
        team.UpdateMemberRole(userId, Role.Admin);

        // Assert
        team.Members[userId].Should().Be(Role.Admin);
        team.UpdatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(1));
    }

    [Fact]
    public void UpdateMemberRole_NotMember_ThrowsException()
    {
        // Arrange
        var team = CreateTestTeam();
        var userId = Guid.NewGuid().ToString();

        // Act
        var act = () => team.UpdateMemberRole(userId, Role.Admin);

        // Assert
        act.Should().Throw<InvalidOperationException>()
            .WithMessage($"User {userId} is not a member of the team.");
    }

    [Fact]
    public void Archive_SetsArchivedFlag()
    {
        // Arrange
        var team = CreateTestTeam();

        // Act
        team.Archive();

        // Assert
        team.UpdatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(1));
    }

    [Fact]
    public void UpdateProfile_UpdatesProperties()
    {
        // Arrange
        var team = CreateTestTeam();
        var newName = TeamName.Create("Updated Team");
        var newDescription = "Updated description";

        // Act
        team.UpdateProfile(newName, newDescription);

        // Assert
        team.Name.Should().Be(newName);
        team.Description.Should().Be(newDescription);
        team.UpdatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(1));
    }

    [Fact]
    public void IsMember_ReturnsTrueForMember()
    {
        // Arrange
        var team = CreateTestTeam();
        var userId = Guid.NewGuid().ToString();
        team.AddMember(userId, Role.Member);

        // Act
        var result = team.IsMember(userId);

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public void IsMember_ReturnsFalseForNonMember()
    {
        // Arrange
        var team = CreateTestTeam();
        var userId = Guid.NewGuid().ToString();

        // Act
        var result = team.IsMember(userId);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public void GetMemberRole_ReturnsRoleForMember()
    {
        // Arrange
        var team = CreateTestTeam();
        var userId = Guid.NewGuid().ToString();
        var role = Role.Admin;
        team.AddMember(userId, role);

        // Act
        var result = team.GetMemberRole(userId);

        // Assert
        result.Should().Be(role);
    }

    [Fact]
    public void GetMemberRole_NotMember_ThrowsException()
    {
        // Arrange
        var team = CreateTestTeam();
        var userId = Guid.NewGuid().ToString();

        // Act
        var act = () => team.GetMemberRole(userId);

        // Assert
        act.Should().Throw<InvalidOperationException>()
            .WithMessage($"User {userId} is not a member of the team.");
    }

    private static Team CreateTestTeam()
    {
        var timeProvider = new TestTimeProvider();
        return Team.Create(
            Guid.NewGuid().ToString(),
            TeamName.Create("Test Team"),
            "A test team",
            Guid.NewGuid().ToString(),
            timeProvider);
    }
}
