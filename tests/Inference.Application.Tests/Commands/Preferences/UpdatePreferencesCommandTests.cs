// <copyright file="UpdatePreferencesCommandTests.cs" company="Synaxis">
// Copyright (c) Synaxis. All rights reserved.
// </copyright>

namespace Inference.Application.Tests.Commands.Preferences;

using System;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Moq;
using Synaxis.Inference.Application.Commands.Preferences;
using Synaxis.Inference.Application.Dtos;
using Synaxis.Inference.Application.Interfaces;
using Synaxis.Inference.Domain.Aggregates;
using Xunit;

/// <summary>
/// Unit tests for <see cref="UpdatePreferencesCommandHandler"/>.
/// </summary>
[Trait("Category", "Unit")]
public class UpdatePreferencesCommandTests
{
    private readonly Mock<IUserChatPreferencesRepository> _repositoryMock;
    private readonly UpdatePreferencesCommandHandler _handler;

    /// <summary>
    /// Initializes a new instance of the <see cref="UpdatePreferencesCommandTests"/> class.
    /// </summary>
    public UpdatePreferencesCommandTests()
    {
        _repositoryMock = new Mock<IUserChatPreferencesRepository>();
        _handler = new UpdatePreferencesCommandHandler(_repositoryMock.Object);
    }

    /// <summary>
    /// Tests that a valid command updates preferences successfully.
    /// </summary>
    [Fact]
    public async Task Handle_ValidCommand_UpdatesPreferences()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var tenantId = Guid.NewGuid();
        var preferencesId = Guid.NewGuid();
        var existingPreferences = CreatePreferences(preferencesId, userId, tenantId);

        var notifications = new NotificationPreferencesDto(
            true, true, 80, false, 100, true);

        var command = new UpdatePreferencesCommand(
            preferencesId,
            userId,
            "System Prompt",
            0.8,
            2048,
            false,
            ResponseFormat.Json,
            "Custom Instructions",
            "dark",
            "es",
            false,
            7,
            notifications);

        _repositoryMock
            .Setup(r => r.GetByIdAsync(preferencesId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingPreferences);

        _repositoryMock
            .Setup(r => r.UpdateAsync(It.IsAny<UserChatPreferences>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.Preferences.Should().NotBeNull();
        result.Preferences.DefaultSystemPrompt.Should().Be("System Prompt");
        result.Preferences.DefaultTemperature.Should().Be(0.8);
        result.Preferences.DefaultMaxTokens.Should().Be(2048);
        result.Preferences.EnableStreamingByDefault.Should().BeFalse();
        result.Preferences.PreferredResponseFormat.Should().Be(ResponseFormat.Json);
        result.Preferences.CustomInstructions.Should().Be("Custom Instructions");
        result.Preferences.ThemePreference.Should().Be("dark");
        result.Preferences.LanguagePreference.Should().Be("es");
        result.Preferences.SaveChatHistory.Should().BeFalse();
        result.Preferences.ChatHistoryRetentionDays.Should().Be(7);

        _repositoryMock.Verify(r => r.UpdateAsync(It.IsAny<UserChatPreferences>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    /// <summary>
    /// Tests that updating non-existent preferences throws an InvalidOperationException.
    /// </summary>
    [Fact]
    public async Task Handle_NonExistentPreferences_ThrowsInvalidOperationException()
    {
        // Arrange
        var notifications = new NotificationPreferencesDto(true, true, 80, false, 100, true);
        var command = new UpdatePreferencesCommand(
            Guid.NewGuid(),
            Guid.NewGuid(),
            null,
            0.7,
            4096,
            true,
            ResponseFormat.Text,
            null,
            "system",
            "en",
            true,
            30,
            notifications);

        _repositoryMock
            .Setup(r => r.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((UserChatPreferences?)null);

        // Act
        Func<Task> act = () => _handler.Handle(command, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*were not found*");
    }

    /// <summary>
    /// Tests that updating another user's preferences throws an UnauthorizedAccessException.
    /// </summary>
    [Fact]
    public async Task Handle_DifferentUser_ThrowsUnauthorizedAccessException()
    {
        // Arrange
        var preferencesUserId = Guid.NewGuid();
        var requestUserId = Guid.NewGuid();
        var preferencesId = Guid.NewGuid();
        var existingPreferences = CreatePreferences(preferencesId, preferencesUserId, Guid.NewGuid());

        var notifications = new NotificationPreferencesDto(true, true, 80, false, 100, true);
        var command = new UpdatePreferencesCommand(
            preferencesId,
            requestUserId,
            null,
            0.7,
            4096,
            true,
            ResponseFormat.Text,
            null,
            "system",
            "en",
            true,
            30,
            notifications);

        _repositoryMock
            .Setup(r => r.GetByIdAsync(preferencesId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingPreferences);

        // Act
        Func<Task> act = () => _handler.Handle(command, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<UnauthorizedAccessException>()
            .WithMessage("*permission*");
    }

    /// <summary>
    /// Tests that invalid temperature throws an ArgumentOutOfRangeException.
    /// </summary>
    [Theory]
    [InlineData(-0.1)]
    [InlineData(2.1)]
    public async Task Handle_InvalidTemperature_ThrowsArgumentOutOfRangeException(double temperature)
    {
        // Arrange
        var notifications = new NotificationPreferencesDto(true, true, 80, false, 100, true);
        var command = new UpdatePreferencesCommand(
            Guid.NewGuid(),
            Guid.NewGuid(),
            null,
            temperature,
            4096,
            true,
            ResponseFormat.Text,
            null,
            "system",
            "en",
            true,
            30,
            notifications);

        // Act
        Func<Task> act = () => _handler.Handle(command, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<ArgumentOutOfRangeException>()
            .WithMessage("*Temperature*");
    }

    /// <summary>
    /// Tests that invalid max tokens throws an ArgumentOutOfRangeException.
    /// </summary>
    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public async Task Handle_InvalidMaxTokens_ThrowsArgumentOutOfRangeException(int maxTokens)
    {
        // Arrange
        var notifications = new NotificationPreferencesDto(true, true, 80, false, 100, true);
        var command = new UpdatePreferencesCommand(
            Guid.NewGuid(),
            Guid.NewGuid(),
            null,
            0.7,
            maxTokens,
            true,
            ResponseFormat.Text,
            null,
            "system",
            "en",
            true,
            30,
            notifications);

        // Act
        Func<Task> act = () => _handler.Handle(command, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<ArgumentOutOfRangeException>()
            .WithMessage("*Max tokens*");
    }

    /// <summary>
    /// Tests that invalid retention days throws an ArgumentOutOfRangeException.
    /// </summary>
    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public async Task Handle_InvalidRetentionDays_ThrowsArgumentOutOfRangeException(int retentionDays)
    {
        // Arrange
        var notifications = new NotificationPreferencesDto(true, true, 80, false, 100, true);
        var command = new UpdatePreferencesCommand(
            Guid.NewGuid(),
            Guid.NewGuid(),
            null,
            0.7,
            4096,
            true,
            ResponseFormat.Text,
            null,
            "system",
            "en",
            true,
            retentionDays,
            notifications);

        // Act
        Func<Task> act = () => _handler.Handle(command, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<ArgumentOutOfRangeException>()
            .WithMessage("*Retention days*");
    }

    private static UserChatPreferences CreatePreferences(Guid id, Guid userId, Guid tenantId)
    {
        return UserChatPreferences.Create(id, userId, tenantId);
    }
}
