// <copyright file="PermissionExtensionsTests.cs" company="Synaxis">
// Copyright (c) Synaxis. All rights reserved.
// </copyright>

namespace Synaxis.Core.Authorization.Tests;

using System.Security.Claims;
using FluentAssertions;
using Xunit;

public class PermissionExtensionsTests
{
    [Fact]
    public void IsOrgAdmin_WithOrgAdminRole_ReturnsTrue()
    {
        // Arrange
        var user = new ClaimsPrincipal(new ClaimsIdentity(new[]
        {
            new Claim(ClaimTypes.Role, AuthorizationPolicies.Roles.OrgAdmin),
        }, "TestAuth"));

        // Act
        var result = user.IsOrgAdmin();

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public void IsOrgAdmin_WithNonOrgAdminRole_ReturnsFalse()
    {
        // Arrange
        var user = new ClaimsPrincipal(new ClaimsIdentity(new[]
        {
            new Claim(ClaimTypes.Role, AuthorizationPolicies.Roles.TeamAdmin),
        }, "TestAuth"));

        // Act
        var result = user.IsOrgAdmin();

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public void IsOrgAdmin_WithNullUser_ReturnsFalse()
    {
        // Arrange
        ClaimsPrincipal user = null!;

        // Act
        var result = user.IsOrgAdmin();

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public void IsTeamAdmin_WithTeamAdminRoleAndTeamId_ReturnsTrue()
    {
        // Arrange
        var teamId = Guid.NewGuid();
        var user = new ClaimsPrincipal(new ClaimsIdentity(new[]
        {
            new Claim(ClaimTypes.Role, AuthorizationPolicies.Roles.TeamAdmin),
            new Claim("team_id", teamId.ToString()),
        }, "TestAuth"));

        // Act
        var result = user.IsTeamAdmin(teamId);

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public void IsTeamAdmin_WithOrgAdminRole_ReturnsTrue()
    {
        // Arrange
        var teamId = Guid.NewGuid();
        var user = new ClaimsPrincipal(new ClaimsIdentity(new[]
        {
            new Claim(ClaimTypes.Role, AuthorizationPolicies.Roles.OrgAdmin),
        }, "TestAuth"));

        // Act
        var result = user.IsTeamAdmin(teamId);

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public void IsTeamAdmin_WithWrongTeamId_ReturnsFalse()
    {
        // Arrange
        var teamId = Guid.NewGuid();
        var wrongTeamId = Guid.NewGuid();
        var user = new ClaimsPrincipal(new ClaimsIdentity(new[]
        {
            new Claim(ClaimTypes.Role, AuthorizationPolicies.Roles.TeamAdmin),
            new Claim("team_id", wrongTeamId.ToString()),
        }, "TestAuth"));

        // Act
        var result = user.IsTeamAdmin(teamId);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public void IsTeamAdmin_WithNullUser_ReturnsFalse()
    {
        // Arrange
        ClaimsPrincipal user = null!;
        var teamId = Guid.NewGuid();

        // Act
        var result = user.IsTeamAdmin(teamId);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public void CanManageTeam_WithTeamAdminRoleAndTeamId_ReturnsTrue()
    {
        // Arrange
        var teamId = Guid.NewGuid();
        var user = new ClaimsPrincipal(new ClaimsIdentity(new[]
        {
            new Claim(ClaimTypes.Role, AuthorizationPolicies.Roles.TeamAdmin),
            new Claim("team_id", teamId.ToString()),
        }, "TestAuth"));

        // Act
        var result = user.CanManageTeam(teamId);

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public void CanManageTeam_WithOrgAdminRole_ReturnsTrue()
    {
        // Arrange
        var teamId = Guid.NewGuid();
        var user = new ClaimsPrincipal(new ClaimsIdentity(new[]
        {
            new Claim(ClaimTypes.Role, AuthorizationPolicies.Roles.OrgAdmin),
        }, "TestAuth"));

        // Act
        var result = user.CanManageTeam(teamId);

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public void CanManageTeam_WithMemberRole_ReturnsFalse()
    {
        // Arrange
        var teamId = Guid.NewGuid();
        var user = new ClaimsPrincipal(new ClaimsIdentity(new[]
        {
            new Claim(ClaimTypes.Role, AuthorizationPolicies.Roles.Member),
            new Claim("team_id", teamId.ToString()),
        }, "TestAuth"));

        // Act
        var result = user.CanManageTeam(teamId);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public void CanInviteMember_WithTeamAdminRoleAndTeamId_ReturnsTrue()
    {
        // Arrange
        var teamId = Guid.NewGuid();
        var user = new ClaimsPrincipal(new ClaimsIdentity(new[]
        {
            new Claim(ClaimTypes.Role, AuthorizationPolicies.Roles.TeamAdmin),
            new Claim("team_id", teamId.ToString()),
        }, "TestAuth"));

        // Act
        var result = user.CanInviteMember(teamId);

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public void CanInviteMember_WithOrgAdminRole_ReturnsTrue()
    {
        // Arrange
        var teamId = Guid.NewGuid();
        var user = new ClaimsPrincipal(new ClaimsIdentity(new[]
        {
            new Claim(ClaimTypes.Role, AuthorizationPolicies.Roles.OrgAdmin),
        }, "TestAuth"));

        // Act
        var result = user.CanInviteMember(teamId);

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public void CanInviteMember_WithMemberRole_ReturnsFalse()
    {
        // Arrange
        var teamId = Guid.NewGuid();
        var user = new ClaimsPrincipal(new ClaimsIdentity(new[]
        {
            new Claim(ClaimTypes.Role, AuthorizationPolicies.Roles.Member),
            new Claim("team_id", teamId.ToString()),
        }, "TestAuth"));

        // Act
        var result = user.CanInviteMember(teamId);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public void GetOrganizationId_WithOrganizationClaim_ReturnsGuid()
    {
        // Arrange
        var orgId = Guid.NewGuid();
        var user = new ClaimsPrincipal(new ClaimsIdentity(new[]
        {
            new Claim("organization_id", orgId.ToString()),
        }, "TestAuth"));

        // Act
        var result = user.GetOrganizationId();

        // Assert
        result.Should().Be(orgId);
    }

    [Fact]
    public void GetOrganizationId_WithInvalidClaimValue_ReturnsNull()
    {
        // Arrange
        var user = new ClaimsPrincipal(new ClaimsIdentity(new[]
        {
            new Claim("organization_id", "not-a-guid"),
        }, "TestAuth"));

        // Act
        var result = user.GetOrganizationId();

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public void GetOrganizationId_WithNoClaim_ReturnsNull()
    {
        // Arrange
        var user = new ClaimsPrincipal(new ClaimsIdentity(new Claim[]
        {
        }, "TestAuth"));

        // Act
        var result = user.GetOrganizationId();

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public void GetOrganizationId_WithNullUser_ReturnsNull()
    {
        // Arrange
        ClaimsPrincipal user = null!;

        // Act
        var result = user.GetOrganizationId();

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public void GetCurrentUserId_WithNameIdentifierClaim_ReturnsGuid()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var user = new ClaimsPrincipal(new ClaimsIdentity(new[]
        {
            new Claim(ClaimTypes.NameIdentifier, userId.ToString()),
        }, "TestAuth"));

        // Act
        var result = user.GetCurrentUserId();

        // Assert
        result.Should().Be(userId);
    }

    [Fact]
    public void GetCurrentUserId_WithSubClaim_ReturnsGuid()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var user = new ClaimsPrincipal(new ClaimsIdentity(new[]
        {
            new Claim("sub", userId.ToString()),
        }, "TestAuth"));

        // Act
        var result = user.GetCurrentUserId();

        // Assert
        result.Should().Be(userId);
    }

    [Fact]
    public void GetCurrentUserId_WithInvalidClaimValue_ReturnsNull()
    {
        // Arrange
        var user = new ClaimsPrincipal(new ClaimsIdentity(new[]
        {
            new Claim(ClaimTypes.NameIdentifier, "not-a-guid"),
        }, "TestAuth"));

        // Act
        var result = user.GetCurrentUserId();

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public void GetCurrentUserId_WithNoClaim_ReturnsNull()
    {
        // Arrange
        var user = new ClaimsPrincipal(new ClaimsIdentity(new Claim[]
        {
        }, "TestAuth"));

        // Act
        var result = user.GetCurrentUserId();

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public void GetCurrentUserId_WithNullUser_ReturnsNull()
    {
        // Arrange
        ClaimsPrincipal user = null!;

        // Act
        var result = user.GetCurrentUserId();

        // Assert
        result.Should().BeNull();
    }
}
