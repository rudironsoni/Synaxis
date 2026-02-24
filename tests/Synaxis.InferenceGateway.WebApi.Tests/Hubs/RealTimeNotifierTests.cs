using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;
using Synaxis.InferenceGateway.WebApi.Hubs;
using Synaxis.InferenceGateway.Application.RealTime;

namespace Synaxis.InferenceGateway.WebApi.Tests.Hubs
{
    public class RealTimeNotifierTests
    {
        [Fact]
        public async Task NotifyProviderHealthChanged_SendsToOrganizationGroup()
        {
            // Arrange
            var mockHubContext = new Mock<IHubContext<SynaxisHub>>();
            var mockLogger = new Mock<ILogger<RealTimeNotifier>>();
            var mockClients = new Mock<IHubClients>();
            var mockClientProxy = new Mock<IClientProxy>();
            
            var organizationId = Guid.NewGuid();
            var update = new ProviderHealthUpdate(
                Guid.NewGuid(),
                "TestProvider",
                true,
                0.95m,
                100,
                DateTime.UtcNow
            );
            
            mockHubContext.Setup(h => h.Clients).Returns(mockClients.Object);
            mockClients.Setup(c => c.Group(organizationId.ToString())).Returns(mockClientProxy.Object);
            
            var notifier = new RealTimeNotifier(mockHubContext.Object, mockLogger.Object);

            // Act
            await notifier.NotifyProviderHealthChanged(organizationId, update);

            // Assert
            mockClientProxy.Verify(
                c => c.SendCoreAsync("ProviderHealthChanged", It.Is<object[]>(o => o[0] == update), default),
                Times.Once
            );
        }

        [Fact]
        public async Task NotifyCostOptimizationApplied_SendsToOrganizationGroup()
        {
            // Arrange
            var mockHubContext = new Mock<IHubContext<SynaxisHub>>();
            var mockLogger = new Mock<ILogger<RealTimeNotifier>>();
            var mockClients = new Mock<IHubClients>();
            var mockClientProxy = new Mock<IClientProxy>();
            
            var organizationId = Guid.NewGuid();
            var result = new CostOptimizationResult(
                organizationId,
                "Provider1",
                "Provider2",
                "Cost savings",
                0.50m,
                DateTime.UtcNow
            );
            
            mockHubContext.Setup(h => h.Clients).Returns(mockClients.Object);
            mockClients.Setup(c => c.Group(organizationId.ToString())).Returns(mockClientProxy.Object);
            
            var notifier = new RealTimeNotifier(mockHubContext.Object, mockLogger.Object);

            // Act
            await notifier.NotifyCostOptimizationApplied(organizationId, result);

            // Assert
            mockClientProxy.Verify(
                c => c.SendCoreAsync("CostOptimizationApplied", It.Is<object[]>(o => o[0] == result), default),
                Times.Once
            );
        }

        [Fact]
        public async Task NotifyModelDiscovered_SendsToOrganizationGroup()
        {
            // Arrange
            var mockHubContext = new Mock<IHubContext<SynaxisHub>>();
            var mockLogger = new Mock<ILogger<RealTimeNotifier>>();
            var mockClients = new Mock<IHubClients>();
            var mockClientProxy = new Mock<IClientProxy>();
            
            var organizationId = Guid.NewGuid();
            var result = new ModelDiscoveryResult(
                Guid.NewGuid(),
                "gpt-4",
                "GPT-4",
                "OpenAI",
                true
            );
            
            mockHubContext.Setup(h => h.Clients).Returns(mockClients.Object);
            mockClients.Setup(c => c.Group(organizationId.ToString())).Returns(mockClientProxy.Object);
            
            var notifier = new RealTimeNotifier(mockHubContext.Object, mockLogger.Object);

            // Act
            await notifier.NotifyModelDiscovered(organizationId, result);

            // Assert
            mockClientProxy.Verify(
                c => c.SendCoreAsync("ModelDiscovered", It.Is<object[]>(o => o[0] == result), default),
                Times.Once
            );
        }

        [Fact]
        public async Task NotifySecurityAlert_SendsToOrganizationGroup()
        {
            // Arrange
            var mockHubContext = new Mock<IHubContext<SynaxisHub>>();
            var mockLogger = new Mock<ILogger<RealTimeNotifier>>();
            var mockClients = new Mock<IHubClients>();
            var mockClientProxy = new Mock<IClientProxy>();
            
            var organizationId = Guid.NewGuid();
            var alert = new SecurityAlert(
                organizationId,
                "weak_secret",
                "critical",
                "Weak API key detected",
                DateTime.UtcNow
            );
            
            mockHubContext.Setup(h => h.Clients).Returns(mockClients.Object);
            mockClients.Setup(c => c.Group(organizationId.ToString())).Returns(mockClientProxy.Object);
            
            var notifier = new RealTimeNotifier(mockHubContext.Object, mockLogger.Object);

            // Act
            await notifier.NotifySecurityAlert(organizationId, alert);

            // Assert
            mockClientProxy.Verify(
                c => c.SendCoreAsync("SecurityAlert", It.Is<object[]>(o => o[0] == alert), default),
                Times.Once
            );
        }

        [Fact]
        public async Task NotifyAuditEvent_SendsToOrganizationGroup()
        {
            // Arrange
            var mockHubContext = new Mock<IHubContext<SynaxisHub>>();
            var mockLogger = new Mock<ILogger<RealTimeNotifier>>();
            var mockClients = new Mock<IHubClients>();
            var mockClientProxy = new Mock<IClientProxy>();
            
            var organizationId = Guid.NewGuid();
            var auditEvent = new AuditEvent(
                Guid.NewGuid(),
                "Create",
                "Provider",
                "user@example.com",
                DateTime.UtcNow
            );
            
            mockHubContext.Setup(h => h.Clients).Returns(mockClients.Object);
            mockClients.Setup(c => c.Group(organizationId.ToString())).Returns(mockClientProxy.Object);
            
            var notifier = new RealTimeNotifier(mockHubContext.Object, mockLogger.Object);

            // Act
            await notifier.NotifyAuditEvent(organizationId, auditEvent);

            // Assert
            mockClientProxy.Verify(
                c => c.SendCoreAsync("AuditEvent", It.Is<object[]>(o => o[0] == auditEvent), default),
                Times.Once
            );
        }

        [Fact]
        public async Task NotifyProviderHealthChanged_HandlesExceptionGracefully()
        {
            // Arrange
            var mockHubContext = new Mock<IHubContext<SynaxisHub>>();
            var mockLogger = new Mock<ILogger<RealTimeNotifier>>();
            var mockClients = new Mock<IHubClients>();
            var mockClientProxy = new Mock<IClientProxy>();
            
            var organizationId = Guid.NewGuid();
            var update = new ProviderHealthUpdate(
                Guid.NewGuid(),
                "TestProvider",
                true,
                0.95m,
                100,
                DateTime.UtcNow
            );
            
            mockHubContext.Setup(h => h.Clients).Returns(mockClients.Object);
            mockClients.Setup(c => c.Group(organizationId.ToString())).Returns(mockClientProxy.Object);
            mockClientProxy.Setup(c => c.SendCoreAsync(It.IsAny<string>(), It.IsAny<object[]>(), default))
                .ThrowsAsync(new Exception("Connection error"));
            
            var notifier = new RealTimeNotifier(mockHubContext.Object, mockLogger.Object);

            // Act - should not throw
            await notifier.NotifyProviderHealthChanged(organizationId, update);

            // Assert - error should be logged
            mockLogger.Verify(
                l => l.Log(
                    LogLevel.Error,
                    It.IsAny<EventId>(),
                    It.IsAny<It.IsAnyType>(),
                    It.IsAny<Exception>(),
                    It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
                Times.Once
            );
        }
    }
}
