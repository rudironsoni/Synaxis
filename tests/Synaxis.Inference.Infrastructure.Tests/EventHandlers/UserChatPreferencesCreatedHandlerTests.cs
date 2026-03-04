// <copyright file="UserChatPreferencesCreatedHandlerTests.cs" company="Synaxis">
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
/// Unit tests for <see cref="UserChatPreferencesCreatedHandler"/>.
/// </summary>
[Trait("Category", "Unit")]
public class UserChatPreferencesCreatedHandlerTests
{
    private readonly Mock<ILogger<UserChatPreferencesCreatedHandler>> _loggerMock;
    private readonly Mock<IAuditService> _auditServiceMock;
    private readonly Mock<IMessageBus> _messageBusMock;
    private readonly UserChatPreferencesCreatedHandler _handler;

    /// <summary>
    /// Initializes a new instance of the <see cref="UserChatPreferencesCreatedHandlerTests"/> class.
    /// </summary>
    public UserChatPreferencesCreatedHandlerTests()
    {
        this._loggerMock = new Mock<ILogger<UserChatPreferencesCreatedHandler>>();
        this._auditServiceMock = new Mock<IAuditService>();
        this._messageBusMock = new Mock<IMessageBus>();
        this._handler = new UserChatPreferencesCreatedHandler(
            this._loggerMock.Object,
            this._auditServiceMock.Object,
            this._messageBusMock.Object);
    }

    [Fact]
    public async Task Handle_WithValidEvent_LogsInformationWithPreferencesDetails()
    {
        // Arrange
        var preferencesId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var tenantId = Guid.NewGuid();
        var notification = new UserChatPreferencesCreated
        {
            PreferencesId = preferencesId,
            UserId = userId,
            TenantId = tenantId,
            PreferredModelId = "gpt-4",
            PreferredProviderId = "openai",
            Timestamp = DateTime.UtcNow,
        };

        // Act
        await this._handler.Handle(notification, CancellationToken.None);

        // Assert
        this._loggerMock.Verify(
            x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString().Contains("User chat preferences created") && v.ToString().Contains(preferencesId.ToString())),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception, string>>()),
            Times.Once);
    }

    [Fact]
    public async Task Handle_WithValidEvent_LogsAuditEventWithUserId()
    {
        // Arrange
        var preferencesId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var tenantId = Guid.NewGuid();
        var notification = new UserChatPreferencesCreated
        {
            PreferencesId = preferencesId,
            UserId = userId,
            TenantId = tenantId,
            PreferredModelId = "gpt-4",
            PreferredProviderId = "openai",
            Timestamp = DateTime.UtcNow,
        };

        // Act
        await this._handler.Handle(notification, CancellationToken.None);

        // Assert
        this._auditServiceMock.Verify(
            x => x.LogEventAsync(It.Is<AuditEvent>(auditEvent =>
                auditEvent.EventType == nameof(UserChatPreferencesCreated) &&
                auditEvent.Action == "Create" &&
                auditEvent.UserId == userId &&
                auditEvent.OrganizationId == tenantId &&
                auditEvent.ResourceId == preferencesId.ToString())),
            Times.Once);
    }

    [Fact]
    public async Task Handle_WithValidEvent_PublishesToMessageBus()
    {
        // Arrange
        var notification = new UserChatPreferencesCreated
        {
            PreferencesId = Guid.NewGuid(),
            UserId = Guid.NewGuid(),
            TenantId = Guid.NewGuid(),
            PreferredModelId = "gpt-4",
            PreferredProviderId = "openai",
            Timestamp = DateTime.UtcNow,
        };

        // Act
        await this._handler.Handle(notification, CancellationToken.None);

        // Assert
        this._messageBusMock.Verify(
            x => x.PublishAsync(
                "preferences.created",
                notification,
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public void Handle_WithNullEvent_ThrowsArgumentNullException()
    {
        // Arrange
        UserChatPreferencesCreated notification = null!;

        // Act
        Func<Task> act = async () => await this._handler.Handle(notification, CancellationToken.None);

        // Assert
        act.Should().ThrowAsync<ArgumentNullException>().WithParameterName("notification");
    }
}
