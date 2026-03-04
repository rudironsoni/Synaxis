// <copyright file="ChatTemplateDeletedHandlerTests.cs" company="Synaxis">
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
/// Unit tests for <see cref="ChatTemplateDeletedHandler"/>.
/// </summary>
[Trait("Category", "Unit")]
public class ChatTemplateDeletedHandlerTests
{
    private readonly Mock<ILogger<ChatTemplateDeletedHandler>> _loggerMock;
    private readonly Mock<IAuditService> _auditServiceMock;
    private readonly Mock<ICacheService> _cacheServiceMock;
    private readonly Mock<IMessageBus> _messageBusMock;
    private readonly ChatTemplateDeletedHandler _handler;

    /// <summary>
    /// Initializes a new instance of the <see cref="ChatTemplateDeletedHandlerTests"/> class.
    /// </summary>
    public ChatTemplateDeletedHandlerTests()
    {
        this._loggerMock = new Mock<ILogger<ChatTemplateDeletedHandler>>();
        this._auditServiceMock = new Mock<IAuditService>();
        this._cacheServiceMock = new Mock<ICacheService>();
        this._messageBusMock = new Mock<IMessageBus>();
        this._handler = new ChatTemplateDeletedHandler(
            this._loggerMock.Object,
            this._auditServiceMock.Object,
            this._cacheServiceMock.Object,
            this._messageBusMock.Object);
    }

    [Fact]
    public async Task Handle_WithValidEvent_CleansUpRelatedData()
    {
        // Arrange
        var templateId = Guid.NewGuid();
        var notification = new ChatTemplateDeleted
        {
            TemplateId = templateId,
            Timestamp = DateTime.UtcNow,
        };

        // Act
        await this._handler.Handle(notification, CancellationToken.None);

        // Assert
        this._cacheServiceMock.Verify(
            x => x.RemoveAsync($"template:usage:{templateId}"),
            Times.Once);
        this._cacheServiceMock.Verify(
            x => x.RemoveAsync($"template:stats:{templateId}"),
            Times.Once);
    }

    [Fact]
    public async Task Handle_WithValidEvent_InvalidatesTemplateCache()
    {
        // Arrange
        var templateId = Guid.NewGuid();
        var notification = new ChatTemplateDeleted
        {
            TemplateId = templateId,
            Timestamp = DateTime.UtcNow,
        };

        // Act
        await this._handler.Handle(notification, CancellationToken.None);

        // Assert
        this._cacheServiceMock.Verify(
            x => x.RemoveAsync($"template:{templateId}"),
            Times.Once);
        this._cacheServiceMock.Verify(
            x => x.RemoveByPatternAsync("templates:list"),
            Times.Once);
    }

    [Fact]
    public async Task Handle_WithCleanupException_LogsErrorButContinues()
    {
        // Arrange
        var templateId = Guid.NewGuid();
        var notification = new ChatTemplateDeleted
        {
            TemplateId = templateId,
            Timestamp = DateTime.UtcNow,
        };

        this._cacheServiceMock
            .Setup(x => x.RemoveAsync($"template:usage:{templateId}"))
            .ThrowsAsync(new Exception("Cleanup error"));

        // Act
        Func<Task> act = async () => await this._handler.Handle(notification, CancellationToken.None);

        // Assert
        await act.Should().NotThrowAsync();
        this._loggerMock.Verify(
            x => x.Log(
                LogLevel.Error,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString().Contains("Failed to cleanup related data")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception, string>>()),
            Times.Once);
    }

    [Fact]
    public async Task Handle_WithValidEvent_LogsAuditEvent()
    {
        // Arrange
        var templateId = Guid.NewGuid();
        var notification = new ChatTemplateDeleted
        {
            TemplateId = templateId,
            Timestamp = DateTime.UtcNow,
        };

        // Act
        await this._handler.Handle(notification, CancellationToken.None);

        // Assert
        this._auditServiceMock.Verify(
            x => x.LogEventAsync(It.Is<AuditEvent>(auditEvent =>
                auditEvent.EventType == nameof(ChatTemplateDeleted) &&
                auditEvent.Action == "Delete" &&
                auditEvent.ResourceId == templateId.ToString())),
            Times.Once);
    }
}
