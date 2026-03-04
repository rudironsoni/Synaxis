// <copyright file="CreatePreferencesCommandTests.cs" company="Synaxis">
// Copyright (c) Synaxis. All rights reserved.
// </copyright>

namespace Inference.Application.Tests.Commands.Preferences;

using System;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Moq;
using Synaxis.Inference.Application.Commands.Preferences;
using Synaxis.Inference.Application.Interfaces;
using Synaxis.Inference.Domain.Aggregates;
using Xunit;

/// <summary>
/// Unit tests for <see cref="CreatePreferencesCommandHandler"/>.
/// </summary>
[Trait("Category", "Unit")]
public class CreatePreferencesCommandTests
{
    private readonly Mock<IUserChatPreferencesRepository> _repositoryMock;
    private readonly CreatePreferencesCommandHandler _handler;

    /// <summary>
    /// Initializes a new instance of the <see cref="CreatePreferencesCommandTests"/> class.
    /// </summary>
    public CreatePreferencesCommandTests()
    {
        _repositoryMock = new Mock<IUserChatPreferencesRepository>();
        _handler = new CreatePreferencesCommandHandler(_repositoryMock.Object);
    }

    /// <summary>
    /// Tests that a valid command creates preferences successfully.
    /// </summary>
    [Fact]
    public async Task Handle_ValidCommand_CreatesPreferences()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var tenantId = Guid.NewGuid();

        var command = new CreatePreferencesCommand(
            userId,
            tenantId,
            "gpt-4",
            "openai");

        _repositoryMock
            .Setup(r => r.ExistsForUserAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        UserChatPreferences? capturedPreferences = null;
        _repositoryMock
            .Setup(r => r.AddAsync(It.IsAny<UserChatPreferences>(), It.IsAny<CancellationToken>()))
            .Callback<UserChatPreferences, CancellationToken>((p, _) => capturedPreferences = p)
            .Returns(Task.CompletedTask);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.PreferencesId.Should().NotBe(Guid.Empty);
        result.Preferences.Should().NotBeNull();
        result.Preferences.UserId.Should().Be(userId);
        result.Preferences.TenantId.Should().Be(tenantId);
        result.Preferences.PreferredModelId.Should().Be("gpt-4");
        result.Preferences.PreferredProviderId.Should().Be("openai");

        _repositoryMock.Verify(r => r.AddAsync(It.IsAny<UserChatPreferences>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    /// <summary>
    /// Tests that creating preferences when they already exist throws an InvalidOperationException.
    /// </summary>
    [Fact]
    public async Task Handle_AlreadyExists_ThrowsInvalidOperationException()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var command = new CreatePreferencesCommand(userId, Guid.NewGuid());

        _repositoryMock
            .Setup(r => r.ExistsForUserAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        // Act
        Func<Task> act = () => _handler.Handle(command, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*already exist*");
    }

    /// <summary>
    /// Tests that preferences can be created with null model and provider.
    /// </summary>
    [Fact]
    public async Task Handle_NullModelAndProvider_CreatesPreferences()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var tenantId = Guid.NewGuid();

        var command = new CreatePreferencesCommand(userId, tenantId);

        _repositoryMock
            .Setup(r => r.ExistsForUserAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        _repositoryMock
            .Setup(r => r.AddAsync(It.IsAny<UserChatPreferences>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.Preferences.PreferredModelId.Should().BeNull();
        result.Preferences.PreferredProviderId.Should().BeNull();
    }

    /// <summary>
    /// Tests that default values are set correctly.
    /// </summary>
    [Fact]
    public async Task Handle_ValidCommand_SetsDefaultValues()
    {
        // Arrange
        var command = new CreatePreferencesCommand(Guid.NewGuid(), Guid.NewGuid());

        _repositoryMock
            .Setup(r => r.ExistsForUserAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        _repositoryMock
            .Setup(r => r.AddAsync(It.IsAny<UserChatPreferences>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.Preferences.DefaultTemperature.Should().Be(0.7);
        result.Preferences.DefaultMaxTokens.Should().Be(4096);
        result.Preferences.EnableStreamingByDefault.Should().BeTrue();
        result.Preferences.PreferredResponseFormat.Should().Be(ResponseFormat.Text);
        result.Preferences.ThemePreference.Should().Be("system");
        result.Preferences.LanguagePreference.Should().Be("en");
        result.Preferences.SaveChatHistory.Should().BeTrue();
        result.Preferences.ChatHistoryRetentionDays.Should().Be(30);
    }
}
