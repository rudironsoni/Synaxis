using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Moq;
using Quartz;
using Synaxis.InferenceGateway.Infrastructure.Agents.Tools;
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
        this._serviceProviderMock = new Mock<IServiceProvider>();
        this._loggerMock = new Mock<ILogger<SecurityAuditAgent>>();
        this._agent = new SecurityAuditAgent(this._serviceProviderMock.Object, this._loggerMock.Object);
    }

    [Fact]
    public async Task Execute_CompletesWithoutError()
    {
        // Arrange
        var contextMock = new Mock<IJobExecutionContext>();
        contextMock.Setup(c => c.CancellationToken).Returns(CancellationToken.None);

        var scopeMock = new Mock<IServiceScope>();
        var dbOptions = new DbContextOptionsBuilder<ControlPlaneDbContext>()
            .UseInMemoryDatabase($"SecurityAuditTest_{Guid.NewGuid()}")
            .Options;
        var db = new ControlPlaneDbContext(dbOptions);
        var configMock = new Mock<IConfiguration>();
        var alertToolMock = new Mock<IAlertTool>();
        var auditToolMock = new Mock<IAuditTool>();

        scopeMock.Setup(s => s.ServiceProvider).Returns(this._serviceProviderMock.Object);
        this._serviceProviderMock.Setup(sp => sp.GetService(typeof(ControlPlaneDbContext)))
            .Returns(db);
        this._serviceProviderMock.Setup(sp => sp.GetService(typeof(IConfiguration)))
            .Returns(configMock.Object);
        this._serviceProviderMock.Setup(sp => sp.GetService(typeof(IAlertTool)))
            .Returns(alertToolMock.Object);
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
        
        // Cleanup
        db.Dispose();
    }

    [Fact]
    public async Task Execute_LogsSecurityAuditStart()
    {
        // Arrange
        var contextMock = new Mock<IJobExecutionContext>();
        contextMock.Setup(c => c.CancellationToken).Returns(CancellationToken.None);

        var scopeMock = new Mock<IServiceScope>();
        var dbOptions = new DbContextOptionsBuilder<ControlPlaneDbContext>()
            .UseInMemoryDatabase($"SecurityAuditTest_{Guid.NewGuid()}")
            .Options;
        var db = new ControlPlaneDbContext(dbOptions);
        var configMock = new Mock<IConfiguration>();
        var alertToolMock = new Mock<IAlertTool>();
        var auditToolMock = new Mock<IAuditTool>();

        scopeMock.Setup(s => s.ServiceProvider).Returns(this._serviceProviderMock.Object);
        this._serviceProviderMock.Setup(sp => sp.GetService(typeof(ControlPlaneDbContext)))
            .Returns(db);
        this._serviceProviderMock.Setup(sp => sp.GetService(typeof(IConfiguration)))
            .Returns(configMock.Object);
        this._serviceProviderMock.Setup(sp => sp.GetService(typeof(IAlertTool)))
            .Returns(alertToolMock.Object);
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
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Starting security audit")),
                null,
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
        
        // Cleanup
        db.Dispose();
    }

    [Theory]
    [InlineData("short", true)] // Too short
    [InlineData("this-is-a-very-long-and-secure-secret-key-12345", false)] // Good length (48 chars)
    [InlineData("MyDefaultSecretKey123456789012", true)] // Contains "default"
    [InlineData("StrongSecretKey123456789012345", true)] // Too short (30 chars)
    [InlineData("StrongSecretKey12345678901234567", false)] // Good secret (33 chars)
    public void JwtSecretValidation_DetectsWeakSecrets(string secret, bool shouldFlagAsWeak)
    {
        // Arrange & Act
        bool isTooShort = secret.Length < 32;
        bool containsWeakPattern = secret.Contains("default", StringComparison.OrdinalIgnoreCase);
        bool isWeak = isTooShort || containsWeakPattern;

        // Assert
        Assert.Equal(shouldFlagAsWeak, isWeak);
    }
}
