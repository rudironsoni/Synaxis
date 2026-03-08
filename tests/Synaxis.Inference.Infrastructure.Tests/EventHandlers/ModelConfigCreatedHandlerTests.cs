// <copyright file="ModelConfigCreatedHandlerTests.cs" company="Synaxis">
// Copyright (c) Synaxis. All rights reserved.
// </copyright>

namespace Synaxis.Inference.Infrastructure.Tests.EventHandlers;

using System;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using Synaxis.Shared.Kernel.Application.Cloud;
using Synaxis.Shared.Kernel.Domain.Contracts;
using Synaxis.Inference.Domain.Aggregates;
using Synaxis.Inference.Domain.Events;
using Synaxis.Inference.Infrastructure.EventHandlers;
using Xunit;

/// <summary>
/// Unit tests for <see cref="ModelConfigCreatedHandler"/>.
/// </summary>
[Trait("Category", "Unit")]
public class ModelConfigCreatedHandlerTests
{
    private readonly Mock<ILogger<ModelConfigCreatedHandler>> _loggerMock;
    private readonly Mock<IAuditService> _auditServiceMock;
    private readonly Mock<IMessageBus> _messageBusMock;
    private readonly ModelConfigCreatedHandler _handler;

    /// <summary>
    /// Initializes a new instance of the <see cref="ModelConfigCreatedHandlerTests"/> class.
    /// </summary>
    public ModelConfigCreatedHandlerTests()
    {
        this._loggerMock = new Mock<ILogger<ModelConfigCreatedHandler>>();
        this._auditServiceMock = new Mock<IAuditService>();
        this._messageBusMock = new Mock<IMessageBus>();
        this._handler = new ModelConfigCreatedHandler(
            this._loggerMock.Object,
            this._auditServiceMock.Object,
            this._messageBusMock.Object);
    }

    [Fact]
    public async Task Handle_WithValidEvent_LogsInformationWithModelDetails()
    {
        // Arrange
        var configId = Guid.NewGuid();
        var tenantId = Guid.NewGuid();
        var notification = new ModelConfigCreated
        {
            ConfigId = configId,
            ModelId = "gpt-4",
            ProviderId = "openai",
            DisplayName = "GPT-4",
            Description = "Test model",
            Settings = new ModelSettings(),
            Pricing = new ModelPricing(),
            Capabilities = new ModelCapabilities(),
            TenantId = tenantId,
            Timestamp = DateTime.UtcNow,
        };

        // Act
        await this._handler.Handle(notification, CancellationToken.None);

        // Assert
        this._loggerMock.Verify(
            x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString().Contains("Model configuration created") && v.ToString().Contains("GPT-4")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception, string>>()),
            Times.Once);
    }

    [Fact]
    public async Task Handle_WithEmptyModelId_LogsWarning()
    {
        // Arrange
        var notification = new ModelConfigCreated
        {
            ConfigId = Guid.NewGuid(),
            ModelId = "",
            ProviderId = "openai",
            DisplayName = "GPT-4",
            Settings = new ModelSettings(),
            Pricing = new ModelPricing(),
            Capabilities = new ModelCapabilities(),
            TenantId = Guid.NewGuid(),
            Timestamp = DateTime.UtcNow,
        };

        // Act
        await this._handler.Handle(notification, CancellationToken.None);

        // Assert
        this._loggerMock.Verify(
            x => x.Log(
                LogLevel.Warning,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString().Contains("has empty model identifier")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception, string>>()),
            Times.Once);
    }

    [Fact]
    public async Task Handle_WithEmptyProviderId_LogsWarning()
    {
        // Arrange
        var notification = new ModelConfigCreated
        {
            ConfigId = Guid.NewGuid(),
            ModelId = "gpt-4",
            ProviderId = "",
            DisplayName = "GPT-4",
            Settings = new ModelSettings(),
            Pricing = new ModelPricing(),
            Capabilities = new ModelCapabilities(),
            TenantId = Guid.NewGuid(),
            Timestamp = DateTime.UtcNow,
        };

        // Act
        await this._handler.Handle(notification, CancellationToken.None);

        // Assert
        this._loggerMock.Verify(
            x => x.Log(
                LogLevel.Warning,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString().Contains("has empty provider identifier")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception, string>>()),
            Times.Once);
    }

    [Fact]
    public async Task Handle_WithNullPricing_LogsWarning()
    {
        // Arrange
        var notification = new ModelConfigCreated
        {
            ConfigId = Guid.NewGuid(),
            ModelId = "gpt-4",
            ProviderId = "openai",
            DisplayName = "GPT-4",
            Settings = new ModelSettings(),
            Pricing = null!,
            Capabilities = new ModelCapabilities(),
            TenantId = Guid.NewGuid(),
            Timestamp = DateTime.UtcNow,
        };

        // Act
        await this._handler.Handle(notification, CancellationToken.None);

        // Assert
        this._loggerMock.Verify(
            x => x.Log(
                LogLevel.Warning,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString().Contains("has no pricing information")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception, string>>()),
            Times.Once);
    }

    [Fact]
    public async Task Handle_WithValidEvent_PublishesToMessageBus()
    {
        // Arrange
        var notification = new ModelConfigCreated
        {
            ConfigId = Guid.NewGuid(),
            ModelId = "gpt-4",
            ProviderId = "openai",
            DisplayName = "GPT-4",
            Settings = new ModelSettings(),
            Pricing = new ModelPricing(),
            Capabilities = new ModelCapabilities(),
            TenantId = Guid.NewGuid(),
            Timestamp = DateTime.UtcNow,
        };

        // Act
        await this._handler.Handle(notification, CancellationToken.None);

        // Assert
        this._messageBusMock.Verify(
            x => x.PublishAsync(
                "models.created",
                notification,
                It.IsAny<CancellationToken>()),
            Times.Once);
    }
}
