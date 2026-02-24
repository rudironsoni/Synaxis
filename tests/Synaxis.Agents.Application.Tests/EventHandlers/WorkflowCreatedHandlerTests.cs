// <copyright file="WorkflowCreatedHandlerTests.cs" company="Synaxis">
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
/// Unit tests for <see cref="WorkflowCreatedHandler"/>.
/// </summary>
public class WorkflowCreatedHandlerTests
{
    private readonly Mock<ILogger<WorkflowCreatedHandler>> _loggerMock;
    private readonly Mock<IAuditService> _auditServiceMock;
    private readonly WorkflowCreatedHandler _handler;

    /// <summary>
    /// Initializes a new instance of the <see cref="WorkflowCreatedHandlerTests"/> class.
    /// </summary>
    public WorkflowCreatedHandlerTests()
    {
        this._loggerMock = new Mock<ILogger<WorkflowCreatedHandler>>();
        this._auditServiceMock = new Mock<IAuditService>();
        this._handler = new WorkflowCreatedHandler(
            this._loggerMock.Object,
            this._auditServiceMock.Object);
    }

    [Fact]
    public async Task Handle_WithValidEvent_LogsInformationWithWorkflowDetails()
    {
        // Arrange
        var workflowId = Guid.NewGuid();
        var tenantId = "tenant-123";
        var notification = new WorkflowCreated
        {
            Id = workflowId,
            Name = "TestWorkflow",
            WorkflowYaml = "test: yaml",
            TenantId = tenantId,
            Version = 1,
        };

        // Act
        await this._handler.Handle(notification, CancellationToken.None);

        // Assert
        this._loggerMock.Verify(
            x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString().Contains("Workflow created") && v.ToString().Contains("TestWorkflow") && v.ToString().Contains(workflowId.ToString()) && v.ToString().Contains(tenantId)),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception, string>>()),
            Times.Once);
    }

    [Fact]
    public async Task Handle_WithValidEvent_CallsAuditServiceLogEventAsync()
    {
        // Arrange
        var workflowId = Guid.NewGuid();
        var tenantId = "tenant-123";
        var notification = new WorkflowCreated
        {
            Id = workflowId,
            Name = "TestWorkflow",
            WorkflowYaml = "test: yaml",
            TenantId = tenantId,
            Version = 1,
        };

        // Act
        await this._handler.Handle(notification, CancellationToken.None);

        // Assert
        this._auditServiceMock.Verify(
            x => x.LogEventAsync(It.Is<AuditEvent>(auditEvent =>
                auditEvent.EventType == "Workflow.Created" &&
                auditEvent.EventCategory == "Workflow" &&
                auditEvent.Action == "Create" &&
                auditEvent.ResourceType == "Workflow" &&
                auditEvent.ResourceId == workflowId.ToString())),
            Times.Once);
    }
}
