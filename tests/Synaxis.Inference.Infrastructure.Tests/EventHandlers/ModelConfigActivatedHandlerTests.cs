// <copyright file="ModelConfigActivatedHandlerTests.cs" company="Synaxis">
// Copyright (c) Synaxis. All rights reserved.
// </copyright>

namespace Synaxis.Inference.Infrastructure.Tests.EventHandlers;

using System;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using Synaxis.Abstractions.Cloud;
using Synaxis.Core.Contracts;
using Synaxis.Inference.Domain.Events;
using Synaxis.Inference.Infrastructure.EventHandlers;
using Xunit;

/// <summary>
/// Unit tests for <see cref="ModelConfigActivatedHandler"/>.
/// </summary>
[Trait("Category", "Unit")]
public class ModelConfigActivatedHandlerTests
{
    private readonly Mock<ILogger<ModelConfigActivatedHandler>> _loggerMock;
    private readonly Mock<IAuditService> _auditServiceMock;
    private readonly Mock<ICacheService> _cacheServiceMock;
    private readonly Mock<IMessageBus> _messageBusMock;
    private readonly ModelConfigActivatedHandler _handler;

    /// <summary>
    /// Initializes a new instance of the <see cref="ModelConfigActivatedHandlerTests"/> class.
    /// </summary>
    public ModelConfigActivatedHandlerTests()
    {
        this._loggerMock = new Mock<ILogger<ModelConfigActivatedHandler>>();
        this._auditServiceMock = new Mock<IAuditService>();
        this._cacheServiceMock = new Mock<ICacheService>();
        this._messageBusMock = new Mock<IMessageBus>();
        this._handler = new ModelConfigActivatedHandler(
            this._loggerMock.Object,
            this._auditServiceMock.Object,
            this._cacheServiceMock.Object,
            this._messageBusMock.Object);
    }

    [Fact]
    public async Task Handle_WithValidEvent_EnablesForRouting()
    {
        // Arrange
        var configId = Guid.NewGuid();
        var notification = new ModelConfigActivated
        {
            ConfigId = configId,
            Timestamp = DateTime.UtcNow,
        };

        // Act
        await this._handler.Handle(notification, CancellationToken.None);

        // Assert
        this._cacheServiceMock.Verify(
            x => x.RemoveByPatternAsync("routing:active-models"),
            Times.Once);
        this._cacheServiceMock.Verify(
            x => x.RemoveAsync($"modelconfig:{configId}"),
            Times.Once);
    }

    [Fact]
    public async Task Handle_WithValidEvent_LogsAuditEvent()
    {
        // Arrange
        var configId = Guid.NewGuid();
        var notification = new ModelConfigActivated
        {
            ConfigId = configId,
            Timestamp = DateTime.UtcNow,
        };

        // Act
        await this._handler.Handle(notification, CancellationToken.None);

        // Assert
        this._auditServiceMock.Verify(
            x => x.LogEventAsync(It.Is<AuditEvent>(auditEvent =>
                auditEvent.EventType == nameof(ModelConfigActivated) &&
                auditEvent.Action == "Activate" &&
                auditEvent.ResourceId == configId.ToString())),
            Times.Once);
    }

    [Fact]
    public async Task Handle_WithCacheException_LogsErrorButContinues()
    {
        // Arrange
        var configId = Guid.NewGuid();
        var notification = new ModelConfigActivated
        {
            ConfigId = configId,
            Timestamp = DateTime.UtcNow,
        };

        this._cacheServiceMock
            .Setup(x => x.RemoveByPatternAsync(It.IsAny<string>()))
            .ThrowsAsync(new Exception("Cache error"));

        // Act
        Func<Task> act = async () => await this._handler.Handle(notification, CancellationToken.None);

        // Assert
        await act.Should().NotThrowAsync();
        this._loggerMock.Verify(
            x => x.Log(
                LogLevel.Error,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString().Contains("Failed to enable model")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception, string>>()),
            Times.Once);
    }

    [Fact]
    public async Task Handle_WithValidEvent_PublishesToMessageBus()
    {
        // Arrange
        var configId = Guid.NewGuid();
        var notification = new ModelConfigActivated
        {
            ConfigId = configId,
            Timestamp = DateTime.UtcNow,
        };

        // Act
        await this._handler.Handle(notification, CancellationToken.None);

        // Assert
        this._messageBusMock.Verify(
            x => x.PublishAsync(
                "models.activated",
                notification,
                It.IsAny<CancellationToken>()),
            Times.Once);
    }
}
