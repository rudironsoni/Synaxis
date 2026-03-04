// <copyright file="ChatTemplateUpdatedHandlerTests.cs" company="Synaxis">
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
/// Unit tests for <see cref="ChatTemplateUpdatedHandler"/>.
/// </summary>
[Trait("Category", "Unit")]
public class ChatTemplateUpdatedHandlerTests
{
    private readonly Mock<ILogger<ChatTemplateUpdatedHandler>> _loggerMock;
    private readonly Mock<IAuditService> _auditServiceMock;
    private readonly Mock<ICacheService> _cacheServiceMock;
    private readonly Mock<IMessageBus> _messageBusMock;
    private readonly ChatTemplateUpdatedHandler _handler;

    /// <summary>
    /// Initializes a new instance of the <see cref="ChatTemplateUpdatedHandlerTests"/> class.
    /// </summary>
    public ChatTemplateUpdatedHandlerTests()
    {
        this._loggerMock = new Mock<ILogger<ChatTemplateUpdatedHandler>>();
        this._auditServiceMock = new Mock<IAuditService>();
        this._cacheServiceMock = new Mock<ICacheService>();
        this._messageBusMock = new Mock<IMessageBus>();
        this._handler = new ChatTemplateUpdatedHandler(
            this._loggerMock.Object,
            this._auditServiceMock.Object,
            this._cacheServiceMock.Object,
            this._messageBusMock.Object);
    }

    [Fact]
    public async Task Handle_WithValidEvent_InvalidatesTemplateCache()
    {
        // Arrange
        var templateId = Guid.NewGuid();
        var notification = new ChatTemplateUpdated
        {
            TemplateId = templateId,
            Name = "TestTemplate",
            Description = "Test Description",
            TemplateContent = "Updated Content",
            Parameters = [],
            Category = "TestCategory",
            Timestamp = DateTime.UtcNow,
        };

        // Act
        await this._handler.Handle(notification, CancellationToken.None);

        // Assert
        this._cacheServiceMock.Verify(
            x => x.RemoveAsync($"template:{templateId}"),
            Times.Once);
    }

    [Fact]
    public async Task Handle_WithValidEvent_InvalidatesCategoryCache()
    {
        // Arrange
        var notification = new ChatTemplateUpdated
        {
            TemplateId = Guid.NewGuid(),
            Name = "TestTemplate",
            Description = "Test Description",
            TemplateContent = "Updated Content",
            Parameters = [],
            Category = "TestCategory",
            Timestamp = DateTime.UtcNow,
        };

        // Act
        await this._handler.Handle(notification, CancellationToken.None);

        // Assert
        this._cacheServiceMock.Verify(
            x => x.RemoveByPatternAsync("templates:category:TestCategory"),
            Times.Once);
    }

    [Fact]
    public async Task Handle_WithValidEvent_InvalidatesListCache()
    {
        // Arrange
        var notification = new ChatTemplateUpdated
        {
            TemplateId = Guid.NewGuid(),
            Name = "TestTemplate",
            Description = "Test Description",
            TemplateContent = "Updated Content",
            Parameters = [],
            Category = "TestCategory",
            Timestamp = DateTime.UtcNow,
        };

        // Act
        await this._handler.Handle(notification, CancellationToken.None);

        // Assert
        this._cacheServiceMock.Verify(
            x => x.RemoveByPatternAsync("templates:list"),
            Times.Once);
    }

    [Fact]
    public async Task Handle_WithCacheException_LogsErrorButContinues()
    {
        // Arrange
        var templateId = Guid.NewGuid();
        var notification = new ChatTemplateUpdated
        {
            TemplateId = templateId,
            Name = "TestTemplate",
            Description = "Test Description",
            TemplateContent = "Updated Content",
            Parameters = [],
            Category = "TestCategory",
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
                It.Is<It.IsAnyType>((v, t) => v.ToString().Contains("Failed to invalidate cache")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception, string>>()),
            Times.Once);
    }

    [Fact]
    public async Task Handle_WithValidEvent_PublishesToMessageBus()
    {
        // Arrange
        var notification = new ChatTemplateUpdated
        {
            TemplateId = Guid.NewGuid(),
            Name = "TestTemplate",
            Description = "Test Description",
            TemplateContent = "Updated Content",
            Parameters = [],
            Category = "TestCategory",
            Timestamp = DateTime.UtcNow,
        };

        // Act
        await this._handler.Handle(notification, CancellationToken.None);

        // Assert
        this._messageBusMock.Verify(
            x => x.PublishAsync(
                "templates.updated",
                notification,
                It.IsAny<CancellationToken>()),
            Times.Once);
    }
}
