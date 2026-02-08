// <copyright file="ViewerAuthorizationHandlerTests.cs" company="Synaxis">
// Copyright (c) Synaxis. All rights reserved.
// </copyright>

namespace Synaxis.Core.Authorization.Tests;

using System.Security.Claims;
using FluentAssertions;
using Microsoft.AspNetCore.Authorization;
using Xunit;

public class ViewerAuthorizationHandlerTests
{
    private readonly ViewerAuthorizationHandler _handler;

    public ViewerAuthorizationHandlerTests()
    {
        this._handler = new ViewerAuthorizationHandler();
    }

    [Fact]
    public async Task HandleRequirementAsync_WithViewerRole_Succeeds()
    {
        // Arrange
        var user = new ClaimsPrincipal(new ClaimsIdentity(new[]
        {
            new Claim(ClaimTypes.Role, AuthorizationPolicies.Roles.Viewer),
        }, "TestAuth"));

        var requirement = new ViewerRequirement();
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
    public async Task HandleRequirementAsync_WithMemberRole_Succeeds()
    {
        // Arrange
        var user = new ClaimsPrincipal(new ClaimsIdentity(new[]
        {
            new Claim(ClaimTypes.Role, AuthorizationPolicies.Roles.Member),
        }, "TestAuth"));

        var requirement = new ViewerRequirement();
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
        }, "TestAuth"));

        var requirement = new ViewerRequirement();
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
        }, "TestAuth"));

        var requirement = new ViewerRequirement();
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
    public async Task HandleRequirementAsync_WithAnyRoleClaim_Succeeds()
    {
        // Arrange
        var user = new ClaimsPrincipal(new ClaimsIdentity(new[]
        {
            new Claim(ClaimTypes.Role, "AnyRole"),
        }, "TestAuth"));

        var requirement = new ViewerRequirement();
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
    public async Task HandleRequirementAsync_WithNoRoleClaim_Fails()
    {
        // Arrange
        var user = new ClaimsPrincipal(new ClaimsIdentity(new Claim[]
        {
        }, "TestAuth"));

        var requirement = new ViewerRequirement();
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
        var requirement = new ViewerRequirement();
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
            new Claim(ClaimTypes.Role, AuthorizationPolicies.Roles.Viewer),
        }));

        var requirement = new ViewerRequirement();
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
    public void ViewerRequirement_ShouldBeIAuthorizationRequirement()
    {
        // Arrange & Act
        var requirement = new ViewerRequirement();

        // Assert
        requirement.Should().BeAssignableTo<IAuthorizationRequirement>();
    }

    [Fact]
    public void Handler_ShouldBeAuthorizationHandler()
    {
        // Arrange & Act
        var handler = new ViewerAuthorizationHandler();

        // Assert
        handler.Should().BeAssignableTo<IAuthorizationHandler>();
    }
}
