// <copyright file="ModelConfigDeactivatedHandlerTests.cs" company="Synaxis">
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
/// Unit tests for <see cref="ModelConfigDeactivatedHandler"/>.
/// </summary>
[Trait("Category", "Unit")]
public class ModelConfigDeactivatedHandlerTests
{
    private readonly Mock<ILogger<ModelConfigDeactivatedHandler>> _loggerMock;
    private readonly Mock<IAuditService> _auditServiceMock;
    private readonly Mock<ICacheService> _cacheServiceMock;
    private readonly Mock<IMessageBus> _messageBusMock;
    private readonly ModelConfigDeactivatedHandler _handler;

    /// <summary>
    /// Initializes a new instance of the <see cref="ModelConfigDeactivatedHandlerTests"/> class.
    /// </summary>
    public ModelConfigDeactivatedHandlerTests()
    {
        this._loggerMock = new Mock<ILogger<ModelConfigDeactivatedHandler>>();
        this._auditServiceMock = new Mock<IAuditService>();
        this._cacheServiceMock = new Mock<ICacheService>();
        this._messageBusMock = new Mock<IMessageBus>();
        this._handler = new ModelConfigDeactivatedHandler(
            this._loggerMock.Object,
            this._auditServiceMock.Object,
            this._cacheServiceMock.Object,
            this._messageBusMock.Object);
    }

    [Fact]
    public async Task Handle_WithValidEvent_DisablesForRouting()
    {
        // Arrange
        var configId = Guid.NewGuid();
        var notification = new ModelConfigDeactivated
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
        var notification = new ModelConfigDeactivated
        {
            ConfigId = configId,
            Timestamp = DateTime.UtcNow,
        };

        // Act
        await this._handler.Handle(notification, CancellationToken.None);

        // Assert
        this._auditServiceMock.Verify(
            x => x.LogEventAsync(It.Is<AuditEvent>(auditEvent =>
                auditEvent.EventType == nameof(ModelConfigDeactivated) &&
                auditEvent.Action == "Deactivate" &&
                auditEvent.ResourceId == configId.ToString())),
            Times.Once);
    }

    [Fact]
    public async Task Handle_WithValidEvent_PublishesToMessageBus()
    {
        // Arrange
        var configId = Guid.NewGuid();
        var notification = new ModelConfigDeactivated
        {
            ConfigId = configId,
            Timestamp = DateTime.UtcNow,
        };

        // Act
        await this._handler.Handle(notification, CancellationToken.None);

        // Assert
        this._messageBusMock.Verify(
            x => x.PublishAsync(
                "models.deactivated",
                notification,
                It.IsAny<CancellationToken>()),
            Times.Once);
    }
}
