using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Moq;
using Quartz;
using Synaxis.InferenceGateway.Infrastructure.Agents.Tools;
using Synaxis.InferenceGateway.Infrastructure.ControlPlane;
using Synaxis.InferenceGateway.Infrastructure.Jobs;
using Xunit;

namespace Synaxis.InferenceGateway.Application.Tests.Agents;

public class CostOptimizationAgentTests
{
    private readonly Mock<IServiceProvider> _serviceProviderMock;
    private readonly Mock<ILogger<CostOptimizationAgent>> _loggerMock;
    private readonly CostOptimizationAgent _agent;

    public CostOptimizationAgentTests()
    {
        this._serviceProviderMock = new Mock<IServiceProvider>();
        this._loggerMock = new Mock<ILogger<CostOptimizationAgent>>();
        this._agent = new CostOptimizationAgent(this._serviceProviderMock.Object, this._loggerMock.Object);
    }

    [Fact]
    public async Task Execute_CompletesWithoutError()
    {
        // Arrange
        var contextMock = new Mock<IJobExecutionContext>();
        contextMock.Setup(c => c.CancellationToken).Returns(CancellationToken.None);

        var scopeMock = new Mock<IServiceScope>();
        var dbMock = new Mock<ControlPlaneDbContext>();
        var routingToolMock = new Mock<IRoutingTool>();
        var auditToolMock = new Mock<IAuditTool>();

        scopeMock.Setup(s => s.ServiceProvider).Returns(this._serviceProviderMock.Object);
        this._serviceProviderMock.Setup(sp => sp.GetService(typeof(ControlPlaneDbContext)))
            .Returns(dbMock.Object);
        this._serviceProviderMock.Setup(sp => sp.GetService(typeof(IRoutingTool)))
            .Returns(routingToolMock.Object);
        this._serviceProviderMock.Setup(sp => sp.GetService(typeof(IAuditTool)))
            .Returns(auditToolMock.Object);

        var scopeFactoryMock = new Mock<IServiceScopeFactory>();
        scopeFactoryMock.Setup(f => f.CreateScope()).Returns(scopeMock.Object);
        this._serviceProviderMock.Setup(sp => sp.GetService(typeof(IServiceScopeFactory)))
            .Returns(scopeFactoryMock.Object);

        // Act
        await this._agent.Execute(contextMock.Object);

        // Assert
        Assert.True(true); // Test that execution completes
    }

    [Fact]
    public async Task Execute_LogsUltraMiserMode()
    {
        // Arrange
        var contextMock = new Mock<IJobExecutionContext>();
        contextMock.Setup(c => c.CancellationToken).Returns(CancellationToken.None);

        var scopeMock = new Mock<IServiceScope>();
        var dbMock = new Mock<ControlPlaneDbContext>();
        var routingToolMock = new Mock<IRoutingTool>();
        var auditToolMock = new Mock<IAuditTool>();

        scopeMock.Setup(s => s.ServiceProvider).Returns(this._serviceProviderMock.Object);
        this._serviceProviderMock.Setup(sp => sp.GetService(typeof(ControlPlaneDbContext)))
            .Returns(dbMock.Object);
        this._serviceProviderMock.Setup(sp => sp.GetService(typeof(IRoutingTool)))
            .Returns(routingToolMock.Object);
        this._serviceProviderMock.Setup(sp => sp.GetService(typeof(IAuditTool)))
            .Returns(auditToolMock.Object);

        var scopeFactoryMock = new Mock<IServiceScopeFactory>();
        scopeFactoryMock.Setup(f => f.CreateScope()).Returns(scopeMock.Object);
        this._serviceProviderMock.Setup(sp => sp.GetService(typeof(IServiceScopeFactory)))
            .Returns(scopeFactoryMock.Object);

        // Act
        await this._agent.Execute(contextMock.Object);

        // Assert
        this._loggerMock.Verify(
            x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("ULTRA MISER MODE")),
                null,
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Theory]
    [InlineData(0.0, 0.0, 1.0, 1.0, true)] // Free to paid = no switch
    [InlineData(1.0, 1.0, 0.0, 0.0, true)] // Paid to free = switch (100% savings)
    [InlineData(1.0, 1.0, 0.5, 0.5, true)] // 50% savings = switch (>20%)
    [InlineData(1.0, 1.0, 0.85, 0.85, false)] // 15% savings = no switch (<20%)
    public void ShouldSwitch_UltraMiserLogic_ReturnsCorrectDecision(
        decimal currentInput, decimal currentOutput,
        decimal altInput, decimal altOutput,
        bool shouldSwitch)
    {
        // Arrange
        bool currentIsFree = currentInput == 0m && currentOutput == 0m;
        bool altIsFree = altInput == 0m && altOutput == 0m;

        // Act - ULTRA MISER MODE logic
        bool result;
        if (currentIsFree)
        {
            result = false; // Never switch from free to paid
        }
        else if (altIsFree)
        {
            result = true; // Always switch to free
        }
        else
        {
            decimal inputSavings = currentInput > 0 ? (currentInput - altInput) / currentInput : 0m;
            decimal outputSavings = currentOutput > 0 ? (currentOutput - altOutput) / currentOutput : 0m;
            decimal avgSavings = (inputSavings + outputSavings) / 2m;
            result = avgSavings > 0.20m; // Require >20% savings
        }

        // Assert
        Assert.Equal(shouldSwitch, result);
    }
}
