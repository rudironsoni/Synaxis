// <copyright file="AgentActivatedHandlerTests.cs" company="Synaxis">
// Copyright (c) Synaxis. All rights reserved.
// </copyright>

namespace Synaxis.Agents.Application.Tests.EventHandlers;

using System;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using Synaxis.Agents.Application.EventHandlers;
using Synaxis.Agents.Domain.Events;
using Xunit;

/// <summary>
/// Unit tests for <see cref="AgentActivatedHandler"/>.
/// </summary>
public class AgentActivatedHandlerTests
{
    private readonly Mock<ILogger<AgentActivatedHandler>> _loggerMock;
    private readonly AgentActivatedHandler _handler;

    /// <summary>
    /// Initializes a new instance of the <see cref="AgentActivatedHandlerTests"/> class.
    /// </summary>
    public AgentActivatedHandlerTests()
    {
        this._loggerMock = new Mock<ILogger<AgentActivatedHandler>>();
        this._handler = new AgentActivatedHandler(this._loggerMock.Object);
    }

    [Fact]
    public async Task Handle_WithValidEvent_LogsInformationWithAgentId()
    {
        // Arrange
        var agentId = Guid.NewGuid();
        var notification = new AgentActivated
        {
            AgentId = agentId,
            Version = 1,
            Timestamp = DateTimeOffset.UtcNow,
        };

        // Act
        await this._handler.Handle(notification, CancellationToken.None);

        // Assert
        this._loggerMock.Verify(
            x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString().Contains("Agent activated") && v.ToString().Contains(agentId.ToString())),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception, string>>()),
            Times.Once);
    }
}
