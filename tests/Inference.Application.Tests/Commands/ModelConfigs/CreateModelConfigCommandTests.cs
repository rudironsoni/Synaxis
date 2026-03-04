// <copyright file="CreateModelConfigCommandTests.cs" company="Synaxis">
// Copyright (c) Synaxis. All rights reserved.
// </copyright>

namespace Inference.Application.Tests.Commands.ModelConfigs;

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Moq;
using Synaxis.Inference.Application.Commands.ModelConfigs;
using Synaxis.Inference.Application.Dtos;
using Synaxis.Inference.Application.Interfaces;
using Synaxis.Inference.Domain.Aggregates;
using Xunit;

/// <summary>
/// Unit tests for <see cref="CreateModelConfigCommandHandler"/>.
/// </summary>
[Trait("Category", "Unit")]
public class CreateModelConfigCommandTests
{
    private readonly Mock<IModelConfigRepository> _repositoryMock;
    private readonly CreateModelConfigCommandHandler _handler;

    /// <summary>
    /// Initializes a new instance of the <see cref="CreateModelConfigCommandTests"/> class.
    /// </summary>
    public CreateModelConfigCommandTests()
    {
        _repositoryMock = new Mock<IModelConfigRepository>();
        _handler = new CreateModelConfigCommandHandler(_repositoryMock.Object);
    }

    /// <summary>
    /// Tests that a valid command creates a model configuration successfully.
    /// </summary>
    [Fact]
    public async Task Handle_ValidCommand_CreatesModelConfig()
    {
        // Arrange
        var tenantId = Guid.NewGuid();
        var settings = new ModelSettingsDto(4096, 0.7, 1.0, 0.0, 0.0, 8192, new[] { "stop" });
        var pricing = new ModelPricingDto(0.01m, 0.02m, false, null);
        var capabilities = new ModelCapabilitiesDto(true, true, false, true, new[] { "en", "es" });

        var command = new CreateModelConfigCommand(
            tenantId,
            "gpt-4",
            "openai",
            "GPT-4",
            "Advanced model",
            settings,
            pricing,
            capabilities);

        _repositoryMock
            .Setup(r => r.ExistsAsync(tenantId, "gpt-4", "openai", null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        ModelConfig? capturedConfig = null;
        _repositoryMock
            .Setup(r => r.AddAsync(It.IsAny<ModelConfig>(), It.IsAny<CancellationToken>()))
            .Callback<ModelConfig, CancellationToken>((c, _) => capturedConfig = c)
            .Returns(Task.CompletedTask);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.ConfigId.Should().NotBe(Guid.Empty);
        result.Config.Should().NotBeNull();
        result.Config.ModelId.Should().Be("gpt-4");
        result.Config.ProviderId.Should().Be("openai");
        result.Config.DisplayName.Should().Be("GPT-4");
        result.Config.Description.Should().Be("Advanced model");
        result.Config.TenantId.Should().Be(tenantId);
        result.Config.IsActive.Should().BeTrue();

        _repositoryMock.Verify(r => r.AddAsync(It.IsAny<ModelConfig>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    /// <summary>
    /// Tests that an empty model ID throws an ArgumentException.
    /// </summary>
    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public async Task Handle_EmptyModelId_ThrowsArgumentException(string? modelId)
    {
        // Arrange
        var command = new CreateModelConfigCommand(
            Guid.NewGuid(),
            modelId!,
            "provider",
            "Name",
            null,
            new ModelSettingsDto(4096, 0.7, 1.0, 0.0, 0.0, 8192, Array.Empty<string>()),
            new ModelPricingDto(0.01m, 0.02m, false, null),
            new ModelCapabilitiesDto(true, false, false, false, Array.Empty<string>()));

        // Act
        Func<Task> act = () => _handler.Handle(command, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<ArgumentException>()
            .WithMessage("*Model ID*");
    }

    /// <summary>
    /// Tests that an empty provider ID throws an ArgumentException.
    /// </summary>
    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public async Task Handle_EmptyProviderId_ThrowsArgumentException(string? providerId)
    {
        // Arrange
        var command = new CreateModelConfigCommand(
            Guid.NewGuid(),
            "model",
            providerId!,
            "Name",
            null,
            new ModelSettingsDto(4096, 0.7, 1.0, 0.0, 0.0, 8192, Array.Empty<string>()),
            new ModelPricingDto(0.01m, 0.02m, false, null),
            new ModelCapabilitiesDto(true, false, false, false, Array.Empty<string>()));

        // Act
        Func<Task> act = () => _handler.Handle(command, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<ArgumentException>()
            .WithMessage("*Provider ID*");
    }

    /// <summary>
    /// Tests that an empty display name throws an ArgumentException.
    /// </summary>
    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public async Task Handle_EmptyDisplayName_ThrowsArgumentException(string? displayName)
    {
        // Arrange
        var command = new CreateModelConfigCommand(
            Guid.NewGuid(),
            "model",
            "provider",
            displayName!,
            null,
            new ModelSettingsDto(4096, 0.7, 1.0, 0.0, 0.0, 8192, Array.Empty<string>()),
            new ModelPricingDto(0.01m, 0.02m, false, null),
            new ModelCapabilitiesDto(true, false, false, false, Array.Empty<string>()));

        // Act
        Func<Task> act = () => _handler.Handle(command, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<ArgumentException>()
            .WithMessage("*Display name*");
    }

    /// <summary>
    /// Tests that duplicate configuration throws an InvalidOperationException.
    /// </summary>
    [Fact]
    public async Task Handle_DuplicateConfig_ThrowsInvalidOperationException()
    {
        // Arrange
        var tenantId = Guid.NewGuid();
        var command = new CreateModelConfigCommand(
            tenantId,
            "gpt-4",
            "openai",
            "GPT-4",
            null,
            new ModelSettingsDto(4096, 0.7, 1.0, 0.0, 0.0, 8192, Array.Empty<string>()),
            new ModelPricingDto(0.01m, 0.02m, false, null),
            new ModelCapabilitiesDto(true, false, false, false, Array.Empty<string>()));

        _repositoryMock
            .Setup(r => r.ExistsAsync(tenantId, "gpt-4", "openai", null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        // Act
        Func<Task> act = () => _handler.Handle(command, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*already exists*");
    }

    /// <summary>
    /// Tests that settings are correctly mapped.
    /// </summary>
    [Fact]
    public async Task Handle_ValidCommand_MapsSettingsCorrectly()
    {
        // Arrange
        var settings = new ModelSettingsDto(2048, 0.5, 0.9, 0.5, 0.5, 4096, new[] { "stop1", "stop2" });
        var pricing = new ModelPricingDto(0.01m, 0.02m, false, null);
        var capabilities = new ModelCapabilitiesDto(true, true, true, true, new[] { "en" });

        var command = new CreateModelConfigCommand(
            Guid.NewGuid(),
            "model",
            "provider",
            "Name",
            null,
            settings,
            pricing,
            capabilities);

        _repositoryMock
            .Setup(r => r.ExistsAsync(It.IsAny<Guid>(), It.IsAny<string>(), It.IsAny<string>(), null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        _repositoryMock
            .Setup(r => r.AddAsync(It.IsAny<ModelConfig>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.Config.Settings.MaxTokens.Should().Be(2048);
        result.Config.Settings.Temperature.Should().Be(0.5);
        result.Config.Settings.TopP.Should().Be(0.9);
        result.Config.Settings.FrequencyPenalty.Should().Be(0.5);
        result.Config.Settings.PresencePenalty.Should().Be(0.5);
        result.Config.Settings.ContextWindow.Should().Be(4096);
        result.Config.Settings.StopSequences.Should().BeEquivalentTo(new[] { "stop1", "stop2" });
    }

    /// <summary>
    /// Tests that values are trimmed.
    /// </summary>
    [Fact]
    public async Task Handle_InputWithWhitespace_TrimsValues()
    {
        // Arrange
        var tenantId = Guid.NewGuid();
        var command = new CreateModelConfigCommand(
            tenantId,
            "  gpt-4  ",
            "  openai  ",
            "  GPT-4  ",
            "  Description  ",
            new ModelSettingsDto(4096, 0.7, 1.0, 0.0, 0.0, 8192, Array.Empty<string>()),
            new ModelPricingDto(0.01m, 0.02m, false, null),
            new ModelCapabilitiesDto(true, false, false, false, Array.Empty<string>()));

        _repositoryMock
            .Setup(r => r.ExistsAsync(tenantId, "gpt-4", "openai", null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        _repositoryMock
            .Setup(r => r.AddAsync(It.IsAny<ModelConfig>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.Config.ModelId.Should().Be("gpt-4");
        result.Config.ProviderId.Should().Be("openai");
        result.Config.DisplayName.Should().Be("GPT-4");
    }
}
