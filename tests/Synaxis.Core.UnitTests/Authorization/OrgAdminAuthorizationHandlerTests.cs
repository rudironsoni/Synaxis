// <copyright file="OrgAdminAuthorizationHandlerTests.cs" company="Synaxis">
// Copyright (c) Synaxis. All rights reserved.
// </copyright>

namespace Synaxis.Core.Authorization.Tests;

using System.Security.Claims;
using FluentAssertions;
using Microsoft.AspNetCore.Authorization;
using Moq;
using Xunit;

public class OrgAdminAuthorizationHandlerTests
{
    private readonly OrgAdminAuthorizationHandler _handler;

    public OrgAdminAuthorizationHandlerTests()
    {
        _handler = new OrgAdminAuthorizationHandler();
    }

    [Fact]
    public async Task HandleRequirementAsync_WithOrgAdminClaim_Succeeds()
    {
        // Arrange
        var user = new ClaimsPrincipal(new ClaimsIdentity(new[]
        {
            new Claim(ClaimTypes.Role, AuthorizationPolicies.Roles.OrgAdmin),
            new Claim("organization_id", "123e4567-e89b-12d3-a456-426614174000")
        }, "TestAuth"));

        var requirement = new OrgAdminRequirement();
        var context = new AuthorizationHandlerContext(
            new[] { requirement },
            user,
            null);

        // Act
        await _handler.HandleAsync(context);

        // Assert
        context.HasSucceeded.Should().BeTrue();
    }

    [Fact]
    public async Task HandleRequirementAsync_WithoutOrgAdminClaim_Fails()
    {
        // Arrange
        var user = new ClaimsPrincipal(new ClaimsIdentity(new[]
        {
            new Claim(ClaimTypes.Role, AuthorizationPolicies.Roles.TeamAdmin),
            new Claim("organization_id", "123e4567-e89b-12d3-a456-426614174000")
        }, "TestAuth"));

        var requirement = new OrgAdminRequirement();
        var context = new AuthorizationHandlerContext(
            new[] { requirement },
            user,
            null);

        // Act
        await _handler.HandleAsync(context);

        // Assert
        context.HasSucceeded.Should().BeFalse();
    }

    [Fact]
    public async Task HandleRequirementAsync_WithNullUser_Fails()
    {
        // Arrange
        var requirement = new OrgAdminRequirement();
        var context = new AuthorizationHandlerContext(
            new[] { requirement },
            null!,
            null);

        // Act
        await _handler.HandleAsync(context);

        // Assert
        context.HasSucceeded.Should().BeFalse();
    }

    [Fact]
    public async Task HandleRequirementAsync_WithUnauthenticatedUser_Fails()
    {
        // Arrange
        var user = new ClaimsPrincipal(new ClaimsIdentity(new[]
        {
            new Claim(ClaimTypes.Role, AuthorizationPolicies.Roles.OrgAdmin)
        })); // No authentication type specified

        var requirement = new OrgAdminRequirement();
        var context = new AuthorizationHandlerContext(
            new[] { requirement },
            user,
            null);

        // Act
        await _handler.HandleAsync(context);

        // Assert
        context.HasSucceeded.Should().BeFalse();
    }

    [Fact]
    public async Task HandleRequirementAsync_WithMissingOrganizationId_Fails()
    {
        // Arrange
        var user = new ClaimsPrincipal(new ClaimsIdentity(new[]
        {
            new Claim(ClaimTypes.Role, AuthorizationPolicies.Roles.OrgAdmin)
        }, "TestAuth"));

        var requirement = new OrgAdminRequirement();
        var context = new AuthorizationHandlerContext(
            new[] { requirement },
            user,
            null);

        // Act
        await _handler.HandleAsync(context);

        // Assert
        context.HasSucceeded.Should().BeFalse();
    }

    [Theory]
    [InlineData("TeamAdmin")]
    [InlineData("Member")]
    [InlineData("Viewer")]
    public async Task HandleRequirementAsync_WithWrongRoleClaim_Fails(string wrongRole)
    {
        // Arrange
        var user = new ClaimsPrincipal(new ClaimsIdentity(new[]
        {
            new Claim(ClaimTypes.Role, wrongRole),
            new Claim("organization_id", "123e4567-e89b-12d3-a456-426614174000")
        }, "TestAuth"));

        var requirement = new OrgAdminRequirement();
        var context = new AuthorizationHandlerContext(
            new[] { requirement },
            user,
            null);

        // Act
        await _handler.HandleAsync(context);

        // Assert
        context.HasSucceeded.Should().BeFalse();
    }

    [Fact]
    public void OrgAdminRequirement_ShouldBeIAuthorizationRequirement()
    {
        // Arrange & Act
        var requirement = new OrgAdminRequirement();

        // Assert
        requirement.Should().BeAssignableTo<IAuthorizationRequirement>();
    }

    [Fact]
    public void Handler_ShouldBeAuthorizationHandler()
    {
        // Arrange & Act
        var handler = new OrgAdminAuthorizationHandler();

        // Assert
        handler.Should().BeAssignableTo<IAuthorizationHandler>();
    }
}
