// <copyright file="GetPreferencesQueryTests.cs" company="Synaxis">
// Copyright (c) Synaxis. All rights reserved.
// </copyright>

namespace Inference.Application.Tests.Queries;

using System;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Moq;
using Synaxis.Inference.Application.Interfaces;
using Synaxis.Inference.Application.Queries;
using Synaxis.Inference.Domain.Aggregates;
using Xunit;

/// <summary>
/// Unit tests for <see cref="GetPreferencesQueryHandler"/>.
/// </summary>
[Trait("Category", "Unit")]
public class GetPreferencesQueryTests
{
    private readonly Mock<IUserChatPreferencesRepository> _repositoryMock;
    private readonly GetPreferencesQueryHandler _handler;

    /// <summary>
    /// Initializes a new instance of the <see cref="GetPreferencesQueryTests"/> class.
    /// </summary>
    public GetPreferencesQueryTests()
    {
        _repositoryMock = new Mock<IUserChatPreferencesRepository>();
        _handler = new GetPreferencesQueryHandler(_repositoryMock.Object);
    }

    /// <summary>
    /// Tests that query returns preferences for user.
    /// </summary>
    [Fact]
    public async Task Handle_ExistingPreferences_ReturnsPreferences()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var tenantId = Guid.NewGuid();
        var preferences = UserChatPreferences.Create(Guid.NewGuid(), userId, tenantId, "gpt-4", "openai");

        var command = new GetPreferencesQuery(userId, tenantId);

        _repositoryMock
            .Setup(r => r.GetByTenantAndUserAsync(tenantId, userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(preferences);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.UserId.Should().Be(userId);
        result.TenantId.Should().Be(tenantId);
        result.PreferredModelId.Should().Be("gpt-4");
        result.PreferredProviderId.Should().Be("openai");
    }

    /// <summary>
    /// Tests that query returns null when preferences not found.
    /// </summary>
    [Fact]
    public async Task Handle_NonExistentPreferences_ReturnsNull()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var tenantId = Guid.NewGuid();
        var command = new GetPreferencesQuery(userId, tenantId);

        _repositoryMock
            .Setup(r => r.GetByTenantAndUserAsync(tenantId, userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((UserChatPreferences?)null);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.Should().BeNull();
    }

    /// <summary>
    /// Tests that default values are correctly mapped.
    /// </summary>
    [Fact]
    public async Task Handle_ExistingPreferences_MapsDefaultValues()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var tenantId = Guid.NewGuid();
        var preferences = UserChatPreferences.Create(Guid.NewGuid(), userId, tenantId);

        var command = new GetPreferencesQuery(userId, tenantId);

        _repositoryMock
            .Setup(r => r.GetByTenantAndUserAsync(tenantId, userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(preferences);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.DefaultTemperature.Should().Be(0.7);
        result.DefaultMaxTokens.Should().Be(4096);
        result.EnableStreamingByDefault.Should().BeTrue();
        result.ThemePreference.Should().Be("system");
        result.LanguagePreference.Should().Be("en");
        result.SaveChatHistory.Should().BeTrue();
        result.ChatHistoryRetentionDays.Should().Be(30);
    }
}
