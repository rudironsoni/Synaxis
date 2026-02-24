// <copyright file="AgentConfigurationUpdatedHandlerTests.cs" company="Synaxis">
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
/// Unit tests for <see cref="AgentConfigurationUpdatedHandler"/>.
/// </summary>
public class AgentConfigurationUpdatedHandlerTests
{
    private readonly Mock<ILogger<AgentConfigurationUpdatedHandler>> _loggerMock;
    private readonly AgentConfigurationUpdatedHandler _handler;

    /// <summary>
    /// Initializes a new instance of the <see cref="AgentConfigurationUpdatedHandlerTests"/> class.
    /// </summary>
    public AgentConfigurationUpdatedHandlerTests()
    {
        this._loggerMock = new Mock<ILogger<AgentConfigurationUpdatedHandler>>();
        this._handler = new AgentConfigurationUpdatedHandler(this._loggerMock.Object);
    }

    [Fact]
    public async Task Handle_WithValidEvent_LogsInformationWithAgentId()
    {
        // Arrange
        var agentId = Guid.NewGuid();
        var notification = new AgentConfigurationUpdated
        {
            AgentId = agentId,
            Name = "UpdatedAgent",
            ConfigurationYaml = "updated: yaml",
            Version = 2,
            Timestamp = DateTimeOffset.UtcNow,
        };

        // Act
        await this._handler.Handle(notification, CancellationToken.None);

        // Assert
        this._loggerMock.Verify(
            x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString().Contains("Agent configuration updated") && v.ToString().Contains(agentId.ToString())),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception, string>>()),
            Times.Once);
    }
}
