// <copyright file="AgentConfigurationCreatedHandlerTests.cs" company="Synaxis">
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
using Synaxis.Core.Contracts;
using Xunit;

/// <summary>
/// Unit tests for <see cref="AgentConfigurationCreatedHandler"/>.
/// </summary>
public class AgentConfigurationCreatedHandlerTests
{
    private readonly Mock<ILogger<AgentConfigurationCreatedHandler>> _loggerMock;
    private readonly Mock<IAuditService> _auditServiceMock;
    private readonly AgentConfigurationCreatedHandler _handler;

    /// <summary>
    /// Initializes a new instance of the <see cref="AgentConfigurationCreatedHandlerTests"/> class.
    /// </summary>
    public AgentConfigurationCreatedHandlerTests()
    {
        this._loggerMock = new Mock<ILogger<AgentConfigurationCreatedHandler>>();
        this._auditServiceMock = new Mock<IAuditService>();
        this._handler = new AgentConfigurationCreatedHandler(
            this._loggerMock.Object,
            this._auditServiceMock.Object);
    }

    [Fact]
    public async Task Handle_WithValidEvent_LogsInformationWithAgentNameAndId()
    {
        // Arrange
        var agentId = Guid.NewGuid();
        var notification = new AgentConfigurationCreated
        {
            AgentId = agentId,
            Name = "TestAgent",
            AgentType = "TestType",
            ConfigurationYaml = "test: yaml",
            TenantId = Guid.NewGuid(),
            Version = 1,
        };

        // Act
        await this._handler.Handle(notification, CancellationToken.None);

        // Assert
        this._loggerMock.Verify(
            x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString().Contains("Agent configuration created") && v.ToString().Contains("TestAgent") && v.ToString().Contains(agentId.ToString())),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception, string>>()),
            Times.Once);
    }

    [Fact]
    public async Task Handle_WithValidEvent_CallsAuditServiceLogEventAsync()
    {
        // Arrange
        var agentId = Guid.NewGuid();
        var tenantId = Guid.NewGuid();
        var notification = new AgentConfigurationCreated
        {
            AgentId = agentId,
            Name = "TestAgent",
            AgentType = "TestType",
            ConfigurationYaml = "test: yaml",
            TenantId = tenantId,
            Version = 1,
        };

        // Act
        await this._handler.Handle(notification, CancellationToken.None);

        // Assert
        this._auditServiceMock.Verify(
            x => x.LogEventAsync(It.Is<AuditEvent>(auditEvent =>
                auditEvent.EventType == nameof(AgentConfigurationCreated) &&
                auditEvent.EventCategory == "AgentConfiguration" &&
                auditEvent.Action == "Create" &&
                auditEvent.ResourceType == "AgentConfiguration" &&
                auditEvent.ResourceId == agentId.ToString() &&
                auditEvent.OrganizationId == tenantId)),
            Times.Once);
    }
}
