// <copyright file="ChatTemplateCreatedHandlerTests.cs" company="Synaxis">
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
/// Unit tests for <see cref="ChatTemplateCreatedHandler"/>.
/// </summary>
[Trait("Category", "Unit")]
public class ChatTemplateCreatedHandlerTests
{
    private readonly Mock<ILogger<ChatTemplateCreatedHandler>> _loggerMock;
    private readonly Mock<IAuditService> _auditServiceMock;
    private readonly Mock<IMessageBus> _messageBusMock;
    private readonly ChatTemplateCreatedHandler _handler;

    /// <summary>
    /// Initializes a new instance of the <see cref="ChatTemplateCreatedHandlerTests"/> class.
    /// </summary>
    public ChatTemplateCreatedHandlerTests()
    {
        this._loggerMock = new Mock<ILogger<ChatTemplateCreatedHandler>>();
        this._auditServiceMock = new Mock<IAuditService>();
        this._messageBusMock = new Mock<IMessageBus>();
        this._handler = new ChatTemplateCreatedHandler(
            this._loggerMock.Object,
            this._auditServiceMock.Object,
            this._messageBusMock.Object);
    }

    [Fact]
    public async Task Handle_WithValidEvent_LogsInformationWithTemplateNameAndId()
    {
        // Arrange
        var templateId = Guid.NewGuid();
        var tenantId = Guid.NewGuid();
        var notification = new ChatTemplateCreated
        {
            TemplateId = templateId,
            Name = "TestTemplate",
            Description = "Test Description",
            TemplateContent = "Test Content",
            Parameters = [],
            Category = "TestCategory",
            TenantId = tenantId,
            IsSystemTemplate = false,
            Timestamp = DateTime.UtcNow,
        };

        // Act
        await this._handler.Handle(notification, CancellationToken.None);

        // Assert
        this._loggerMock.Verify(
            x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString().Contains("Chat template created") && v.ToString().Contains("TestTemplate") && v.ToString().Contains(templateId.ToString())),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception, string>>()),
            Times.Once);
    }

    [Fact]
    public async Task Handle_WithValidEvent_CallsAuditServiceLogEventAsync()
    {
        // Arrange
        var templateId = Guid.NewGuid();
        var tenantId = Guid.NewGuid();
        var notification = new ChatTemplateCreated
        {
            TemplateId = templateId,
            Name = "TestTemplate",
            Description = "Test Description",
            TemplateContent = "Test Content",
            Parameters = [],
            Category = "TestCategory",
            TenantId = tenantId,
            IsSystemTemplate = false,
            Timestamp = DateTime.UtcNow,
        };

        // Act
        await this._handler.Handle(notification, CancellationToken.None);

        // Assert
        this._auditServiceMock.Verify(
            x => x.LogEventAsync(It.Is<AuditEvent>(auditEvent =>
                auditEvent.EventType == nameof(ChatTemplateCreated) &&
                auditEvent.EventCategory == "Template" &&
                auditEvent.Action == "Create" &&
                auditEvent.ResourceType == "ChatTemplate" &&
                auditEvent.ResourceId == templateId.ToString() &&
                auditEvent.OrganizationId == tenantId)),
            Times.Once);
    }

    [Fact]
    public async Task Handle_WithValidEvent_PublishesToMessageBus()
    {
        // Arrange
        var templateId = Guid.NewGuid();
        var notification = new ChatTemplateCreated
        {
            TemplateId = templateId,
            Name = "TestTemplate",
            Description = "Test Description",
            TemplateContent = "Test Content",
            Parameters = [],
            Category = "TestCategory",
            TenantId = Guid.NewGuid(),
            IsSystemTemplate = false,
            Timestamp = DateTime.UtcNow,
        };

        // Act
        await this._handler.Handle(notification, CancellationToken.None);

        // Assert
        this._messageBusMock.Verify(
            x => x.PublishAsync(
                "templates.created",
                notification,
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public void Handle_WithNullEvent_ThrowsArgumentNullException()
    {
        // Arrange
        ChatTemplateCreated notification = null!;

        // Act
        Func<Task> act = async () => await this._handler.Handle(notification, CancellationToken.None);

        // Assert
        act.Should().ThrowAsync<ArgumentNullException>().WithParameterName("notification");
    }

    [Fact]
    public async Task Handle_WithMessageBusException_LogsErrorButDoesNotThrow()
    {
        // Arrange
        var templateId = Guid.NewGuid();
        var notification = new ChatTemplateCreated
        {
            TemplateId = templateId,
            Name = "TestTemplate",
            Description = "Test Description",
            TemplateContent = "Test Content",
            Parameters = [],
            Category = "TestCategory",
            TenantId = Guid.NewGuid(),
            IsSystemTemplate = false,
            Timestamp = DateTime.UtcNow,
        };

        this._messageBusMock
            .Setup(x => x.PublishAsync(
                It.IsAny<string>(),
                It.IsAny<ChatTemplateCreated>(),
                It.IsAny<CancellationToken>()))
            .ThrowsAsync(new Exception("Message bus error"));

        // Act
        Func<Task> act = async () => await this._handler.Handle(notification, CancellationToken.None);

        // Assert
        await act.Should().NotThrowAsync();
        this._loggerMock.Verify(
            x => x.Log(
                LogLevel.Error,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString().Contains("Failed to publish template created event")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception, string>>()),
            Times.Once);
    }

    [Fact]
    public async Task Handle_WithoutMessageBus_DoesNotPublish()
    {
        // Arrange
        var handlerWithoutMessageBus = new ChatTemplateCreatedHandler(
            this._loggerMock.Object,
            this._auditServiceMock.Object,
            null);

        var notification = new ChatTemplateCreated
        {
            TemplateId = Guid.NewGuid(),
            Name = "TestTemplate",
            Description = "Test Description",
            TemplateContent = "Test Content",
            Parameters = [],
            Category = "TestCategory",
            TenantId = Guid.NewGuid(),
            IsSystemTemplate = false,
            Timestamp = DateTime.UtcNow,
        };

        // Act
        await handlerWithoutMessageBus.Handle(notification, CancellationToken.None);

        // Assert
        this._messageBusMock.Verify(
            x => x.PublishAsync(
                It.IsAny<string>(),
                It.IsAny<ChatTemplateCreated>(),
                It.IsAny<CancellationToken>()),
            Times.Never);
    }
}
