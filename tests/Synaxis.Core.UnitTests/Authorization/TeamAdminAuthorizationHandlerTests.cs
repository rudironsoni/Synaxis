// <copyright file="TeamAdminAuthorizationHandlerTests.cs" company="Synaxis">
// Copyright (c) Synaxis. All rights reserved.
// </copyright>

namespace Synaxis.Core.Authorization.Tests;

using System.Security.Claims;
using FluentAssertions;
using Microsoft.AspNetCore.Authorization;
using Xunit;

public class TeamAdminAuthorizationHandlerTests
{
    private readonly TeamAdminAuthorizationHandler _handler;
    private readonly Guid _teamId = Guid.Parse("123e4567-e89b-12d3-a456-426614174000");

    public TeamAdminAuthorizationHandlerTests()
    {
        this._handler = new TeamAdminAuthorizationHandler();
    }

    [Fact]
    public async Task HandleRequirementAsync_WithTeamAdminRoleAndTeamMembership_Succeeds()
    {
        // Arrange
        var user = new ClaimsPrincipal(new ClaimsIdentity(new[]
        {
            new Claim(ClaimTypes.Role, AuthorizationPolicies.Roles.TeamAdmin),
            new Claim("team_id", this._teamId.ToString()),
        }, "TestAuth"));

        var requirement = new TeamAdminRequirement(this._teamId);
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

        var requirement = new TeamAdminRequirement(this._teamId);
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
    public async Task HandleRequirementAsync_WithTeamAdminRoleButWrongTeam_Fails()
    {
        // Arrange
        var wrongTeamId = Guid.Parse("999e4567-e89b-12d3-a456-426614174999");
        var user = new ClaimsPrincipal(new ClaimsIdentity(new[]
        {
            new Claim(ClaimTypes.Role, AuthorizationPolicies.Roles.TeamAdmin),
            new Claim("team_id", wrongTeamId.ToString()),
        }, "TestAuth"));

        var requirement = new TeamAdminRequirement(this._teamId);
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
    public async Task HandleRequirementAsync_WithTeamAdminRoleButNoTeamClaim_Fails()
    {
        // Arrange
        var user = new ClaimsPrincipal(new ClaimsIdentity(new[]
        {
            new Claim(ClaimTypes.Role, AuthorizationPolicies.Roles.TeamAdmin),
        }, "TestAuth"));

        var requirement = new TeamAdminRequirement(this._teamId);
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
    public async Task HandleRequirementAsync_WithMemberRole_Fails()
    {
        // Arrange
        var user = new ClaimsPrincipal(new ClaimsIdentity(new[]
        {
            new Claim(ClaimTypes.Role, AuthorizationPolicies.Roles.Member),
            new Claim("team_id", this._teamId.ToString()),
        }, "TestAuth"));

        var requirement = new TeamAdminRequirement(this._teamId);
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

        var requirement = new TeamAdminRequirement(this._teamId);
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
        var requirement = new TeamAdminRequirement(this._teamId);
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
            new Claim(ClaimTypes.Role, AuthorizationPolicies.Roles.TeamAdmin),
            new Claim("team_id", this._teamId.ToString()),
        }));

        var requirement = new TeamAdminRequirement(this._teamId);
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
    public void TeamAdminRequirement_ShouldStoreTeamId()
    {
        // Arrange & Act
        var teamId = Guid.NewGuid();
        var requirement = new TeamAdminRequirement(teamId);

        // Assert
        requirement.TeamId.Should().Be(teamId);
    }

    [Fact]
    public void TeamAdminRequirement_ShouldBeIAuthorizationRequirement()
    {
        // Arrange & Act
        var requirement = new TeamAdminRequirement(Guid.NewGuid());

        // Assert
        requirement.Should().BeAssignableTo<IAuthorizationRequirement>();
    }

    [Fact]
    public void Handler_ShouldBeAuthorizationHandler()
    {
        // Arrange & Act
        var handler = new TeamAdminAuthorizationHandler();

        // Assert
        handler.Should().BeAssignableTo<IAuthorizationHandler>();
    }
}
