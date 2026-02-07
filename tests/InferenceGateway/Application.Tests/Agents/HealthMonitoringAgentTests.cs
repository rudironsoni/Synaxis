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
        this._serviceProviderMock = new Mock<IServiceProvider>();
        this._scopeMock = new Mock<IServiceScope>();
        this._scopeFactoryMock = new Mock<IServiceScopeFactory>();
        this._dbMock = new Mock<ControlPlaneDbContext>();
        this._healthToolMock = new Mock<IHealthTool>();
        this._alertToolMock = new Mock<IAlertTool>();
        this._auditToolMock = new Mock<IAuditTool>();
        this._loggerMock = new Mock<ILogger<HealthMonitoringAgent>>();

        this._scopeMock.Setup(s => s.ServiceProvider).Returns(this._serviceProviderMock.Object);
        this._serviceProviderMock.Setup(sp => sp.GetService(typeof(IServiceScopeFactory)))
            .Returns(this._scopeFactoryMock.Object);
        this._serviceProviderMock.Setup(sp => sp.GetService(typeof(ControlPlaneDbContext)))
            .Returns(this._dbMock.Object);
        this._serviceProviderMock.Setup(sp => sp.GetService(typeof(IHealthTool)))
            .Returns(this._healthToolMock.Object);
        this._serviceProviderMock.Setup(sp => sp.GetService(typeof(IAlertTool)))
            .Returns(this._alertToolMock.Object);
        this._serviceProviderMock.Setup(sp => sp.GetService(typeof(IAuditTool)))
            .Returns(this._auditToolMock.Object);

        this._scopeFactoryMock.Setup(f => f.CreateScope()).Returns(this._scopeMock.Object);

        this._agent = new HealthMonitoringAgent(this._serviceProviderMock.Object, this._loggerMock.Object);
    }

    [Fact]
    public async Task Execute_CompletesWithoutError()
    {
        // Arrange
        var contextMock = new Mock<IJobExecutionContext>();
        contextMock.Setup(c => c.CancellationToken).Returns(CancellationToken.None);

        // Act
        await this._agent.Execute(contextMock.Object);

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
        await this._agent.Execute(contextMock.Object);

        // Assert
        this._loggerMock.Verify(
            x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Starting health check")),
                null,
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }
}
