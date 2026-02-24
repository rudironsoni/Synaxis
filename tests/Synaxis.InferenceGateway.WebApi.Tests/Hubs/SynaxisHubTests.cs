using System;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;
using Synaxis.InferenceGateway.WebApi.Hubs;

namespace Synaxis.InferenceGateway.WebApi.Tests.Hubs
{
    public class SynaxisHubTests
    {
        [Fact]
        public async Task JoinOrganization_AddsConnectionToGroup()
        {
            // Arrange
            var mockLogger = new Mock<ILogger<SynaxisHub>>();
            var mockGroups = new Mock<IGroupManager>();
            var mockContext = new Mock<HubCallerContext>();
            var mockClients = new Mock<IHubCallerClients>();
            
            var connectionId = "test-connection-id";
            var organizationId = "test-org-id";
            
            mockContext.Setup(c => c.ConnectionId).Returns(connectionId);
            mockContext.Setup(c => c.User).Returns(new ClaimsPrincipal(new ClaimsIdentity(new[] { new Claim(ClaimTypes.Name, "testuser") }, "test")));
            
            var hub = new SynaxisHub(mockLogger.Object)
            {
                Context = mockContext.Object,
                Groups = mockGroups.Object,
                Clients = mockClients.Object
            };

            // Act
            await hub.JoinOrganization(organizationId);

            // Assert
            mockGroups.Verify(g => g.AddToGroupAsync(connectionId, organizationId, default), Times.Once);
        }

        [Fact]
        public async Task JoinOrganization_WithEmptyId_ThrowsArgumentException()
        {
            // Arrange
            var mockLogger = new Mock<ILogger<SynaxisHub>>();
            var mockContext = new Mock<HubCallerContext>();
            
            mockContext.Setup(c => c.User).Returns(new ClaimsPrincipal(new ClaimsIdentity(new[] { new Claim(ClaimTypes.Name, "testuser") }, "test")));
            
            var hub = new SynaxisHub(mockLogger.Object)
            {
                Context = mockContext.Object
            };

            // Act & Assert
            await Assert.ThrowsAsync<ArgumentException>(() => hub.JoinOrganization(string.Empty));
        }

        [Fact]
        public async Task LeaveOrganization_RemovesConnectionFromGroup()
        {
            // Arrange
            var mockLogger = new Mock<ILogger<SynaxisHub>>();
            var mockGroups = new Mock<IGroupManager>();
            var mockContext = new Mock<HubCallerContext>();
            var mockClients = new Mock<IHubCallerClients>();
            
            var connectionId = "test-connection-id";
            var organizationId = "test-org-id";
            
            mockContext.Setup(c => c.ConnectionId).Returns(connectionId);
            mockContext.Setup(c => c.User).Returns(new ClaimsPrincipal(new ClaimsIdentity(new[] { new Claim(ClaimTypes.Name, "testuser") }, "test")));
            
            var hub = new SynaxisHub(mockLogger.Object)
            {
                Context = mockContext.Object,
                Groups = mockGroups.Object,
                Clients = mockClients.Object
            };

            // Act
            await hub.LeaveOrganization(organizationId);

            // Assert
            mockGroups.Verify(g => g.RemoveFromGroupAsync(connectionId, organizationId, default), Times.Once);
        }

        [Fact]
        public async Task OnConnectedAsync_WithUnauthenticatedUser_AbortsConnection()
        {
            // Arrange
            var mockLogger = new Mock<ILogger<SynaxisHub>>();
            var mockContext = new Mock<HubCallerContext>();
            
            // User is not authenticated
            mockContext.Setup(c => c.User).Returns(new ClaimsPrincipal(new ClaimsIdentity()));
            mockContext.Setup(c => c.ConnectionId).Returns("test-connection-id");
            
            var hub = new SynaxisHub(mockLogger.Object)
            {
                Context = mockContext.Object
            };

            // Act
            await hub.OnConnectedAsync();

            // Assert
            mockContext.Verify(c => c.Abort(), Times.Once);
        }

        [Fact]
        public async Task OnConnectedAsync_WithAuthenticatedUser_AllowsConnection()
        {
            // Arrange
            var mockLogger = new Mock<ILogger<SynaxisHub>>();
            var mockContext = new Mock<HubCallerContext>();
            
            // User is authenticated
            mockContext.Setup(c => c.User).Returns(new ClaimsPrincipal(new ClaimsIdentity(new[] { new Claim(ClaimTypes.Name, "testuser") }, "test")));
            mockContext.Setup(c => c.ConnectionId).Returns("test-connection-id");
            
            var hub = new SynaxisHub(mockLogger.Object)
            {
                Context = mockContext.Object
            };

            // Act
            await hub.OnConnectedAsync();

            // Assert
            mockContext.Verify(c => c.Abort(), Times.Never);
        }
    }
}
