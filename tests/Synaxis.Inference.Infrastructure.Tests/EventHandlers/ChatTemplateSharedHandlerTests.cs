// <copyright file="ChatTemplateSharedHandlerTests.cs" company="Synaxis">
// Copyright (c) Synaxis. All rights reserved.
// </copyright>

namespace Synaxis.Inference.Infrastructure.Tests.EventHandlers;

using System;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using Synaxis.Core.Contracts;
using Synaxis.Inference.Domain.Events;
using Synaxis.Inference.Infrastructure.EventHandlers;
using Xunit;

/// <summary>
/// Unit tests for <see cref="ChatTemplateSharedHandler"/>.
/// </summary>
[Trait("Category", "Unit")]
public class ChatTemplateSharedHandlerTests
{
    private readonly Mock<ILogger<ChatTemplateSharedHandler>> _loggerMock;
    private readonly Mock<IAuditService> _auditServiceMock;
    private readonly ChatTemplateSharedHandler _handler;

    /// <summary>
    /// Initializes a new instance of the <see cref="ChatTemplateSharedHandlerTests"/> class.
    /// </summary>
    public ChatTemplateSharedHandlerTests()
    {
        this._loggerMock = new Mock<ILogger<ChatTemplateSharedHandler>>();
        this._auditServiceMock = new Mock<IAuditService>();
        this._handler = new ChatTemplateSharedHandler(
            this._loggerMock.Object,
            this._auditServiceMock.Object);
    }

    [Fact]
    public async Task Handle_WithValidEvent_LogsInformationWithTemplateId()
    {
        // Arrange
        var templateId = Guid.NewGuid();
        var notification = new ChatTemplateUsed
        {
            TemplateId = templateId,
            Timestamp = DateTime.UtcNow,
        };

        // Act
        await this._handler.Handle(notification, CancellationToken.None);

        // Assert
        this._loggerMock.Verify(
            x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString().Contains("Chat template used") && v.ToString().Contains(templateId.ToString())),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception, string>>()),
            Times.Once);
    }

    [Fact]
    public async Task Handle_WithValidEvent_SendsNotification()
    {
        // Arrange
        var templateId = Guid.NewGuid();
        var notification = new ChatTemplateUsed
        {
            TemplateId = templateId,
            Timestamp = DateTime.UtcNow,
        };

        // Act
        await this._handler.Handle(notification, CancellationToken.None);

        // Assert
        this._loggerMock.Verify(
            x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString().Contains("Notification: Template")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception, string>>()),
            Times.Once);
    }

    [Fact]
    public async Task Handle_WithValidEvent_LogsAuditEvent()
    {
        // Arrange
        var templateId = Guid.NewGuid();
        var notification = new ChatTemplateUsed
        {
            TemplateId = templateId,
            Timestamp = DateTime.UtcNow,
        };

        // Act
        await this._handler.Handle(notification, CancellationToken.None);

        // Assert
        this._auditServiceMock.Verify(
            x => x.LogEventAsync(It.Is<AuditEvent>(auditEvent =>
                auditEvent.EventType == nameof(ChatTemplateUsed) &&
                auditEvent.Action == "Use" &&
                auditEvent.ResourceId == templateId.ToString())),
            Times.Once);
    }

    [Fact]
    public void Handle_WithNullEvent_ThrowsArgumentNullException()
    {
        // Arrange
        ChatTemplateUsed notification = null!;

        // Act
        Func<Task> act = async () => await this._handler.Handle(notification, CancellationToken.None);

        // Assert
        act.Should().ThrowAsync<ArgumentNullException>().WithParameterName("notification");
    }
}
