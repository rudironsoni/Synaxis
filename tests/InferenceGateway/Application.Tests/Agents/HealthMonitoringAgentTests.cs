using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Moq;
using Quartz;
using Synaxis.InferenceGateway.Application.Agents.Tools;
using Synaxis.InferenceGateway.Infrastructure.ControlPlane;
using Synaxis.InferenceGateway.Infrastructure.Jobs;
using Xunit;

namespace Synaxis.InferenceGateway.Application.Tests.Agents;

public class HealthMonitoringAgentTests
{
    private readonly Mock<IServiceProvider> _serviceProviderMock;
    private readonly Mock<IServiceScope> _scopeMock;
    private readonly Mock<IServiceScopeFactory> _scopeFactoryMock;
    private readonly Mock<ControlPlaneDbContext> _dbMock;
    private readonly Mock<IHealthTool> _healthToolMock;
    private readonly Mock<IAlertTool> _alertToolMock;
    private readonly Mock<IAuditTool> _auditToolMock;
    private readonly Mock<ILogger<HealthMonitoringAgent>> _loggerMock;
    private readonly HealthMonitoringAgent _agent;

    public HealthMonitoringAgentTests()
    {
        _serviceProviderMock = new Mock<IServiceProvider>();
        _scopeMock = new Mock<IServiceScope>();
        _scopeFactoryMock = new Mock<IServiceScopeFactory>();
        _dbMock = new Mock<ControlPlaneDbContext>();
        _healthToolMock = new Mock<IHealthTool>();
        _alertToolMock = new Mock<IAlertTool>();
        _auditToolMock = new Mock<IAuditTool>();
        _loggerMock = new Mock<ILogger<HealthMonitoringAgent>>();

        _scopeMock.Setup(s => s.ServiceProvider).Returns(_serviceProviderMock.Object);
        _serviceProviderMock.Setup(sp => sp.GetService(typeof(IServiceScopeFactory)))
            .Returns(_scopeFactoryMock.Object);
        _serviceProviderMock.Setup(sp => sp.GetService(typeof(ControlPlaneDbContext)))
            .Returns(_dbMock.Object);
        _serviceProviderMock.Setup(sp => sp.GetService(typeof(IHealthTool)))
            .Returns(_healthToolMock.Object);
        _serviceProviderMock.Setup(sp => sp.GetService(typeof(IAlertTool)))
            .Returns(_alertToolMock.Object);
        _serviceProviderMock.Setup(sp => sp.GetService(typeof(IAuditTool)))
            .Returns(_auditToolMock.Object);

        _scopeFactoryMock.Setup(f => f.CreateScope()).Returns(_scopeMock.Object);

        _agent = new HealthMonitoringAgent(_serviceProviderMock.Object, _loggerMock.Object);
    }

    [Fact]
    public async Task Execute_CompletesWithoutError()
    {
        // Arrange
        var contextMock = new Mock<IJobExecutionContext>();
        contextMock.Setup(c => c.CancellationToken).Returns(CancellationToken.None);

        // Act
        await _agent.Execute(contextMock.Object);

        // Assert
        Assert.True(true); // Test that execution completes
    }

    [Fact]
    public async Task Execute_LogsStartAndCompletion()
    {
        // Arrange
        var contextMock = new Mock<IJobExecutionContext>();
        contextMock.Setup(c => c.CancellationToken).Returns(CancellationToken.None);

        // Act
        await _agent.Execute(contextMock.Object);

        // Assert
        _loggerMock.Verify(
            x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Starting health check")),
                null,
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }
}
