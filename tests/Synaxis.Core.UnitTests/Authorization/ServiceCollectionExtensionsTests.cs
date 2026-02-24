// <copyright file="ServiceCollectionExtensionsTests.cs" company="Synaxis">
// Copyright (c) Synaxis. All rights reserved.
// </copyright>

namespace Synaxis.Core.Authorization.Tests;

using FluentAssertions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

public class ServiceCollectionExtensionsTests
{
    [Fact]
    public void AddAuthorizationHandlers_RegistersOrgAdminHandler()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act
        services.AddAuthorizationHandlers();
        var provider = services.BuildServiceProvider();

        // Assert
        var handlers = provider.GetServices<IAuthorizationHandler>();
        handlers.Should().Contain(h => h is OrgAdminAuthorizationHandler);
    }

    [Fact]
    public void AddAuthorizationHandlers_RegistersTeamAdminHandler()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act
        services.AddAuthorizationHandlers();
        var provider = services.BuildServiceProvider();

        // Assert
        var handlers = provider.GetServices<IAuthorizationHandler>();
        handlers.Should().Contain(h => h is TeamAdminAuthorizationHandler);
    }

    [Fact]
    public void AddAuthorizationHandlers_RegistersMemberHandler()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act
        services.AddAuthorizationHandlers();
        var provider = services.BuildServiceProvider();

        // Assert
        var handlers = provider.GetServices<IAuthorizationHandler>();
        handlers.Should().Contain(h => h is MemberAuthorizationHandler);
    }

    [Fact]
    public void AddAuthorizationHandlers_RegistersViewerHandler()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act
        services.AddAuthorizationHandlers();
        var provider = services.BuildServiceProvider();

        // Assert
        var handlers = provider.GetServices<IAuthorizationHandler>();
        handlers.Should().Contain(h => h is ViewerAuthorizationHandler);
    }

    [Fact]
    public void AddAuthorizationHandlers_RegistersAllFourHandlerTypes()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act
        services.AddAuthorizationHandlers();
        var provider = services.BuildServiceProvider();
        var handlers = provider.GetServices<IAuthorizationHandler>().ToList();

        // Assert
        handlers.Should().HaveCount(4);
        handlers.Should().Contain(h => h is OrgAdminAuthorizationHandler);
        handlers.Should().Contain(h => h is TeamAdminAuthorizationHandler);
        handlers.Should().Contain(h => h is MemberAuthorizationHandler);
        handlers.Should().Contain(h => h is ViewerAuthorizationHandler);
    }
}
