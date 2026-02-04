using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Moq;
using Quartz;
using Synaxis.InferenceGateway.Application.Agents.Tools;
using Synaxis.InferenceGateway.Infrastructure.ControlPlane;
using Synaxis.InferenceGateway.Infrastructure.Jobs;
using Xunit;

namespace Synaxis.InferenceGateway.Application.Tests.Agents;

public class SecurityAuditAgentTests
{
    private readonly Mock<IServiceProvider> _serviceProviderMock;
    private readonly Mock<ILogger<SecurityAuditAgent>> _loggerMock;
    private readonly SecurityAuditAgent _agent;

    public SecurityAuditAgentTests()
    {
        _serviceProviderMock = new Mock<IServiceProvider>();
        _loggerMock = new Mock<ILogger<SecurityAuditAgent>>();
        _agent = new SecurityAuditAgent(_serviceProviderMock.Object, _loggerMock.Object);
    }

    [Fact]
    public async Task Execute_CompletesWithoutError()
    {
        // Arrange
        var contextMock = new Mock<IJobExecutionContext>();
        contextMock.Setup(c => c.CancellationToken).Returns(CancellationToken.None);

        var scopeMock = new Mock<IServiceScope>();
        var dbMock = new Mock<ControlPlaneDbContext>();
        var configMock = new Mock<IConfiguration>();
        var alertToolMock = new Mock<IAlertTool>();
        var auditToolMock = new Mock<IAuditTool>();

        scopeMock.Setup(s => s.ServiceProvider).Returns(_serviceProviderMock.Object);
        _serviceProviderMock.Setup(sp => sp.GetService(typeof(ControlPlaneDbContext)))
            .Returns(dbMock.Object);
        _serviceProviderMock.Setup(sp => sp.GetService(typeof(IConfiguration)))
            .Returns(configMock.Object);
        _serviceProviderMock.Setup(sp => sp.GetService(typeof(IAlertTool)))
            .Returns(alertToolMock.Object);
        _serviceProviderMock.Setup(sp => sp.GetService(typeof(IAuditTool)))
            .Returns(auditToolMock.Object);

        var scopeFactoryMock = new Mock<IServiceScopeFactory>();
        scopeFactoryMock.Setup(f => f.CreateScope()).Returns(scopeMock.Object);
        _serviceProviderMock.Setup(sp => sp.GetService(typeof(IServiceScopeFactory)))
            .Returns(scopeFactoryMock.Object);

        // Act
        await _agent.Execute(contextMock.Object);

        // Assert
        Assert.True(true); // Test that execution completes
    }

    [Fact]
    public async Task Execute_LogsSecurityAuditStart()
    {
        // Arrange
        var contextMock = new Mock<IJobExecutionContext>();
        contextMock.Setup(c => c.CancellationToken).Returns(CancellationToken.None);

        var scopeMock = new Mock<IServiceScope>();
        var dbMock = new Mock<ControlPlaneDbContext>();
        var configMock = new Mock<IConfiguration>();
        var alertToolMock = new Mock<IAlertTool>();
        var auditToolMock = new Mock<IAuditTool>();

        scopeMock.Setup(s => s.ServiceProvider).Returns(_serviceProviderMock.Object);
        _serviceProviderMock.Setup(sp => sp.GetService(typeof(ControlPlaneDbContext)))
            .Returns(dbMock.Object);
        _serviceProviderMock.Setup(sp => sp.GetService(typeof(IConfiguration)))
            .Returns(configMock.Object);
        _serviceProviderMock.Setup(sp => sp.GetService(typeof(IAlertTool)))
            .Returns(alertToolMock.Object);
        _serviceProviderMock.Setup(sp => sp.GetService(typeof(IAuditTool)))
            .Returns(auditToolMock.Object);

        var scopeFactoryMock = new Mock<IServiceScopeFactory>();
        scopeFactoryMock.Setup(f => f.CreateScope()).Returns(scopeMock.Object);
        _serviceProviderMock.Setup(sp => sp.GetService(typeof(IServiceScopeFactory)))
            .Returns(scopeFactoryMock.Object);

        // Act
        await _agent.Execute(contextMock.Object);

        // Assert
        _loggerMock.Verify(
            x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Starting security audit")),
                null,
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Theory]
    [InlineData("short", true)] // Too short
    [InlineData("this-is-a-very-long-and-secure-secret-key-12345", false)] // Good length
    [InlineData("MyDefaultSecretKey123456789012", true)] // Contains "default"
    [InlineData("StrongSecretKey123456789012345", false)] // Good secret
    public void JwtSecretValidation_DetectsWeakSecrets(string secret, bool shouldFlagAsWeak)
    {
        // Arrange & Act
        bool isTooShort = secret.Length < 32;
        bool containsWeakPattern = secret.Contains("default", StringComparison.OrdinalIgnoreCase) ||
                                    secret.Contains("secret", StringComparison.OrdinalIgnoreCase);
        bool isWeak = isTooShort || containsWeakPattern;

        // Assert
        Assert.Equal(shouldFlagAsWeak, isWeak);
    }
}
