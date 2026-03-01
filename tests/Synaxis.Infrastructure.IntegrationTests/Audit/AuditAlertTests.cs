// <copyright file="AuditAlertTests.cs" company="Synaxis">
// Copyright (c) Synaxis. All rights reserved.
// </copyright>

namespace Synaxis.Infrastructure.IntegrationTests.Audit;

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using MediatR;
using Microsoft.Extensions.Logging;
using Moq;
using Synaxis.Core.Models;
using Synaxis.Infrastructure.EventHandlers.Audit;
using Xunit;

/// <summary>
/// Integration tests for audit alert functionality.
/// </summary>
[Collection("AuditTests")]
public class AuditAlertTests : IClassFixture<AuditTestFixture>
{
    private readonly AuditTestFixture _fixture;

    /// <summary>
    /// Initializes a new instance of the <see cref="AuditAlertTests"/> class.
    /// </summary>
    /// <param name="fixture">The test fixture.</param>
    public AuditAlertTests(AuditTestFixture fixture)
    {
        _fixture = fixture;
    }

    /// <summary>
    /// Tests that suspicious activity triggers an alert.
    /// </summary>
    [Fact]
    public async Task SuspiciousActivity_TriggersAlert()
    {
        // Arrange
        var mediatorMock = new Mock<IMediator>();
        var loggerMock = new Mock<ILogger<AuditAlertService>>();

        var alertService = new AuditAlertService(
            _fixture.Repository,
            mediatorMock.Object,
            loggerMock.Object);

        // Create a bulk export event (triggers DataExfiltration alert)
        var auditLog = _fixture.CreateAuditLog(
            "data.export",
            "data_access",
            "export",
            "report",
            Guid.NewGuid().ToString());

        // Act
        await alertService.EvaluateForAlertsAsync(auditLog);

        // Assert - verify alert was published
        mediatorMock.Verify(
            m => m.Publish(It.IsAny<AuditAlertEvent>(), It.IsAny<CancellationToken>()),
            Times.Once);
    }

    /// <summary>
    /// Tests that normal activity does not trigger an alert.
    /// </summary>
    [Fact]
    public async Task NormalActivity_DoesNotTriggerAlert()
    {
        // Arrange
        var mediatorMock = new Mock<IMediator>();
        var loggerMock = new Mock<ILogger<AuditAlertService>>();

        var alertService = new AuditAlertService(
            _fixture.Repository,
            mediatorMock.Object,
            loggerMock.Object);

        // Create a normal read event
        var auditLog = _fixture.CreateAuditLog(
            "data.read",
            "data_access",
            "read",
            "document",
            Guid.NewGuid().ToString());

        // Act
        await alertService.EvaluateForAlertsAsync(auditLog);

        // Assert - verify no alert was published
        mediatorMock.Verify(
            m => m.Publish(It.IsAny<AuditAlertEvent>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    /// <summary>
    /// Tests that brute force login detection triggers alert.
    /// </summary>
    [Fact]
    public async Task BruteForceLogin_TriggersAlert()
    {
        // Arrange
        var mediatorMock = new Mock<IMediator>();
        var loggerMock = new Mock<ILogger<AuditAlertService>>();

        var alertService = new AuditAlertService(
            _fixture.Repository,
            mediatorMock.Object,
            loggerMock.Object);

        // Create failed login events to exceed threshold
        for (var i = 0; i < 5; i++)
        {
            var auditLog = new AuditLog
            {
                Id = Guid.NewGuid(),
                OrganizationId = _fixture.TestOrganizationId,
                UserId = _fixture.TestUserId,
                EventType = "auth.login_failed",
                EventCategory = "authentication",
                Action = "failed_login",
                ResourceType = "user",
                ResourceId = _fixture.TestUserId.ToString(),
                Metadata = new Dictionary<string, object>(StringComparer.Ordinal),
                IpAddress = "192.168.1.100",
                UserAgent = "TestAgent/1.0",
                Region = "us-east-1",
                IntegrityHash = "test-hash",
                PreviousHash = string.Empty,
                Timestamp = DateTime.UtcNow,
            };

            await _fixture.Repository.AddAsync(auditLog);
        }

        // Create one more failed login to trigger the alert
        var triggerLog = new AuditLog
        {
            Id = Guid.NewGuid(),
            OrganizationId = _fixture.TestOrganizationId,
            UserId = _fixture.TestUserId,
            EventType = "auth.login_failed",
            EventCategory = "authentication",
            Action = "failed_login",
            ResourceType = "user",
            ResourceId = _fixture.TestUserId.ToString(),
            Metadata = new Dictionary<string, object>(StringComparer.Ordinal),
            IpAddress = "192.168.1.100",
            UserAgent = "TestAgent/1.0",
            Region = "us-east-1",
            IntegrityHash = "test-hash",
            PreviousHash = string.Empty,
            Timestamp = DateTime.UtcNow,
        };

        // Act
        await alertService.EvaluateForAlertsAsync(triggerLog);

        // Assert
        mediatorMock.Verify(
            m => m.Publish(
                It.Is<AuditAlertEvent>(e => e.AlertType == "BruteForceLogin"),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    /// <summary>
    /// Tests that get active alerts returns alerts for organization.
    /// </summary>
    [Fact]
    public async Task GetActiveAlerts_ReturnsAlertsForOrganization()
    {
        // Arrange
        var mediatorMock = new Mock<IMediator>();
        var loggerMock = new Mock<ILogger<AuditAlertService>>();

        var alertService = new AuditAlertService(
            _fixture.Repository,
            mediatorMock.Object,
            loggerMock.Object);

        // Trigger an alert
        var auditLog = _fixture.CreateAuditLog(
            "data.export",
            "data_access",
            "export",
            "report",
            Guid.NewGuid().ToString());

        await alertService.EvaluateForAlertsAsync(auditLog);

        // Act
        var alerts = await alertService.GetActiveAlertsAsync(_fixture.TestOrganizationId);

        // Assert
        alerts.Should().NotBeEmpty();
    }

    /// <summary>
    /// Tests that alert handler logs alerts correctly.
    /// </summary>
    [Fact]
    public async Task AlertHandler_LogsAlertsCorrectly()
    {
        // Arrange
        var loggerMock = new Mock<ILogger<AuditAlertHandler>>();
        var handler = new AuditAlertHandler(loggerMock.Object);

        var alert = new AuditAlertEvent
        {
            OrganizationId = _fixture.TestOrganizationId,
            UserId = _fixture.TestUserId,
            AlertType = "TestAlert",
            Severity = "High",
            Message = "Test alert message",
        };

        // Act
        await handler.Handle(alert, CancellationToken.None);

        // Assert - verify logging occurred (the handler logs based on severity)
        loggerMock.Verify(
            x => x.Log(
                It.IsAny<LogLevel>(),
                It.IsAny<EventId>(),
                It.IsAny<It.IsAnyType>(),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }
}
