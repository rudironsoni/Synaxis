// <copyright file="MemberAuthorizationHandlerTests.cs" company="Synaxis">
// Copyright (c) Synaxis. All rights reserved.
// </copyright>

namespace Synaxis.Core.Authorization.Tests;

using System.Security.Claims;
using FluentAssertions;
using Microsoft.AspNetCore.Authorization;
using Xunit;

public class MemberAuthorizationHandlerTests
{
    private readonly MemberAuthorizationHandler _handler;
    private readonly Guid _teamId = Guid.Parse("123e4567-e89b-12d3-a456-426614174000");

    public MemberAuthorizationHandlerTests()
    {
        this._handler = new MemberAuthorizationHandler();
    }

    [Fact]
    public async Task HandleRequirementAsync_WithMemberRoleAndTeamMembership_Succeeds()
    {
        // Arrange
        var user = new ClaimsPrincipal(new ClaimsIdentity(new[]
        {
            new Claim(ClaimTypes.Role, AuthorizationPolicies.Roles.Member),
            new Claim("team_id", this._teamId.ToString()),
        }, "TestAuth"));

        var requirement = new MemberRequirement(this._teamId);
        var context = new AuthorizationHandlerContext(
            new[] { requirement },
            user,
            null);

        // Act
        await this._handler.HandleAsync(context);

        // Assert
        context.HasSucceeded.Should().BeTrue();
    }

    [Fact]
    public async Task HandleRequirementAsync_WithTeamAdminRole_Succeeds()
    {
        // Arrange
        var user = new ClaimsPrincipal(new ClaimsIdentity(new[]
        {
            new Claim(ClaimTypes.Role, AuthorizationPolicies.Roles.TeamAdmin),
            new Claim("team_id", this._teamId.ToString()),
        }, "TestAuth"));

        var requirement = new MemberRequirement(this._teamId);
        var context = new AuthorizationHandlerContext(
            new[] { requirement },
            user,
            null);

        // Act
        await this._handler.HandleAsync(context);

        // Assert
        context.HasSucceeded.Should().BeTrue();
    }

    [Fact]
    public async Task HandleRequirementAsync_WithOrgAdminRole_Succeeds()
    {
        // Arrange
        var user = new ClaimsPrincipal(new ClaimsIdentity(new[]
        {
            new Claim(ClaimTypes.Role, AuthorizationPolicies.Roles.OrgAdmin),
            new Claim("team_id", this._teamId.ToString()),
        }, "TestAuth"));

        var requirement = new MemberRequirement(this._teamId);
        var context = new AuthorizationHandlerContext(
            new[] { requirement },
            user,
            null);

        // Act
        await this._handler.HandleAsync(context);

        // Assert
        context.HasSucceeded.Should().BeTrue();
    }

    [Fact]
    public async Task HandleRequirementAsync_WithMemberRoleButWrongTeam_Fails()
    {
        // Arrange
        var wrongTeamId = Guid.Parse("999e4567-e89b-12d3-a456-426614174999");
        var user = new ClaimsPrincipal(new ClaimsIdentity(new[]
        {
            new Claim(ClaimTypes.Role, AuthorizationPolicies.Roles.Member),
            new Claim("team_id", wrongTeamId.ToString()),
        }, "TestAuth"));

        var requirement = new MemberRequirement(this._teamId);
        var context = new AuthorizationHandlerContext(
            new[] { requirement },
            user,
            null);

        // Act
        await this._handler.HandleAsync(context);

        // Assert
        context.HasSucceeded.Should().BeFalse();
    }

    [Fact]
    public async Task HandleRequirementAsync_WithMemberRoleButNoTeamClaim_Fails()
    {
        // Arrange
        var user = new ClaimsPrincipal(new ClaimsIdentity(new[]
        {
            new Claim(ClaimTypes.Role, AuthorizationPolicies.Roles.Member),
        }, "TestAuth"));

        var requirement = new MemberRequirement(this._teamId);
        var context = new AuthorizationHandlerContext(
            new[] { requirement },
            user,
            null);

        // Act
        await this._handler.HandleAsync(context);

        // Assert
        context.HasSucceeded.Should().BeFalse();
    }

    [Fact]
    public async Task HandleRequirementAsync_WithViewerRole_Fails()
    {
        // Arrange
        var user = new ClaimsPrincipal(new ClaimsIdentity(new[]
        {
            new Claim(ClaimTypes.Role, AuthorizationPolicies.Roles.Viewer),
            new Claim("team_id", this._teamId.ToString()),
        }, "TestAuth"));

        var requirement = new MemberRequirement(this._teamId);
        var context = new AuthorizationHandlerContext(
            new[] { requirement },
            user,
            null);

        // Act
        await this._handler.HandleAsync(context);

        // Assert
        context.HasSucceeded.Should().BeFalse();
    }

    [Fact]
    public async Task HandleRequirementAsync_WithNullUser_Fails()
    {
        // Arrange
        var requirement = new MemberRequirement(this._teamId);
        var context = new AuthorizationHandlerContext(
            new[] { requirement },
            null!,
            null);

        // Act
        await this._handler.HandleAsync(context);

        // Assert
        context.HasSucceeded.Should().BeFalse();
    }

    [Fact]
    public async Task HandleRequirementAsync_WithUnauthenticatedUser_Fails()
    {
        // Arrange
        var user = new ClaimsPrincipal(new ClaimsIdentity(new[]
        {
            new Claim(ClaimTypes.Role, AuthorizationPolicies.Roles.Member),
            new Claim("team_id", this._teamId.ToString()),
        }));

        var requirement = new MemberRequirement(this._teamId);
        var context = new AuthorizationHandlerContext(
            new[] { requirement },
            user,
            null);

        // Act
        await this._handler.HandleAsync(context);

        // Assert
        context.HasSucceeded.Should().BeFalse();
    }

    [Fact]
    public void MemberRequirement_ShouldStoreTeamId()
    {
        // Arrange & Act
        var teamId = Guid.NewGuid();
        var requirement = new MemberRequirement(teamId);

        // Assert
        requirement.TeamId.Should().Be(teamId);
    }

    [Fact]
    public void MemberRequirement_ShouldBeIAuthorizationRequirement()
    {
        // Arrange & Act
        var requirement = new MemberRequirement(Guid.NewGuid());

        // Assert
        requirement.Should().BeAssignableTo<IAuthorizationRequirement>();
    }

    [Fact]
    public void Handler_ShouldBeAuthorizationHandler()
    {
        // Arrange & Act
        var handler = new MemberAuthorizationHandler();

        // Assert
        handler.Should().BeAssignableTo<IAuthorizationHandler>();
    }
}
