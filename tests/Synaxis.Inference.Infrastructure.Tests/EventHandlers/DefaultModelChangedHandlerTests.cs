// <copyright file="DefaultModelChangedHandlerTests.cs" company="Synaxis">
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
using Synaxis.Inference.Domain.Events;
using Synaxis.Inference.Infrastructure.EventHandlers;
using Xunit;

/// <summary>
/// Unit tests for <see cref="DefaultModelChangedHandler"/>.
/// </summary>
[Trait("Category", "Unit")]
public class DefaultModelChangedHandlerTests
{
    private readonly Mock<ILogger<DefaultModelChangedHandler>> _loggerMock;
    private readonly Mock<IAuditService> _auditServiceMock;
    private readonly Mock<ICacheService> _cacheServiceMock;
    private readonly Mock<IMessageBus> _messageBusMock;
    private readonly DefaultModelChangedHandler _handler;

    /// <summary>
    /// Initializes a new instance of the <see cref="DefaultModelChangedHandlerTests"/> class.
    /// </summary>
    public DefaultModelChangedHandlerTests()
    {
        this._loggerMock = new Mock<ILogger<DefaultModelChangedHandler>>();
        this._auditServiceMock = new Mock<IAuditService>();
        this._cacheServiceMock = new Mock<ICacheService>();
        this._messageBusMock = new Mock<IMessageBus>();
        this._handler = new DefaultModelChangedHandler(
            this._loggerMock.Object,
            this._auditServiceMock.Object,
            this._cacheServiceMock.Object,
            this._messageBusMock.Object);
    }

    [Fact]
    public async Task Handle_WithValidEvent_UpdatesRoutingPreferences()
    {
        // Arrange
        var preferencesId = Guid.NewGuid();
        var notification = new PreferredModelUpdated
        {
            PreferencesId = preferencesId,
            ModelId = "gpt-4",
            ProviderId = "openai",
            Timestamp = DateTime.UtcNow,
        };

        // Act
        await this._handler.Handle(notification, CancellationToken.None);

        // Assert
        this._cacheServiceMock.Verify(
            x => x.RemoveAsync($"routing:preferences:{preferencesId}"),
            Times.Once);
        this._cacheServiceMock.Verify(
            x => x.RemoveByPatternAsync("routing:default-model"),
            Times.Once);
        this._cacheServiceMock.Verify(
            x => x.RemoveAsync($"user-routing:{preferencesId}"),
            Times.Once);
    }

    [Fact]
    public async Task Handle_WithValidEvent_LogsAuditEventWithActionDefaultModelChanged()
    {
        // Arrange
        var preferencesId = Guid.NewGuid();
        var notification = new PreferredModelUpdated
        {
            PreferencesId = preferencesId,
            ModelId = "gpt-4",
            ProviderId = "openai",
            Timestamp = DateTime.UtcNow,
        };

        // Act
        await this._handler.Handle(notification, CancellationToken.None);

        // Assert
        this._auditServiceMock.Verify(
            x => x.LogEventAsync(It.Is<AuditEvent>(auditEvent =>
                auditEvent.EventType == nameof(PreferredModelUpdated) &&
                auditEvent.Action == "DefaultModelChanged" &&
                auditEvent.ResourceId == preferencesId.ToString())),
            Times.Once);
    }

    [Fact]
    public async Task Handle_WithCacheException_LogsErrorButContinues()
    {
        // Arrange
        var preferencesId = Guid.NewGuid();
        var notification = new PreferredModelUpdated
        {
            PreferencesId = preferencesId,
            ModelId = "gpt-4",
            ProviderId = "openai",
            Timestamp = DateTime.UtcNow,
        };

        this._cacheServiceMock
            .Setup(x => x.RemoveAsync(It.IsAny<string>()))
            .ThrowsAsync(new Exception("Cache error"));

        // Act
        Func<Task> act = async () => await this._handler.Handle(notification, CancellationToken.None);

        // Assert
        await act.Should().NotThrowAsync();
        this._loggerMock.Verify(
            x => x.Log(
                LogLevel.Error,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString().Contains("Failed to update routing preferences")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception, string>>()),
            Times.Once);
    }

    [Fact]
    public async Task Handle_WithValidEvent_PublishesToMessageBus()
    {
        // Arrange
        var notification = new PreferredModelUpdated
        {
            PreferencesId = Guid.NewGuid(),
            ModelId = "gpt-4",
            ProviderId = "openai",
            Timestamp = DateTime.UtcNow,
        };

        // Act
        await this._handler.Handle(notification, CancellationToken.None);

        // Assert
        this._messageBusMock.Verify(
            x => x.PublishAsync(
                "preferences.default-model-changed",
                notification,
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public void Handle_WithNullEvent_ThrowsArgumentNullException()
    {
        // Arrange
        PreferredModelUpdated notification = null!;

        // Act
        Func<Task> act = async () => await this._handler.Handle(notification, CancellationToken.None);

        // Assert
        act.Should().ThrowAsync<ArgumentNullException>().WithParameterName("notification");
    }
}
