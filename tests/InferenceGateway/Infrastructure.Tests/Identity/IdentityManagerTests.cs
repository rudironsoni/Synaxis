using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Moq;
using Xunit;
using Microsoft.Extensions.Logging;
using Synaxis.InferenceGateway.Infrastructure.Identity.Core;

namespace Synaxis.InferenceGateway.Infrastructure.Tests.Identity
{
    public class IdentityManagerTests
    {
        [Fact]
        public async Task StartAuth_FindsStrategyAndInitiatesFlow()
        {
            var mockStrat = new Mock<IAuthStrategy>();
            mockStrat.Setup(s => s.InitiateFlowAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(new AuthResult { Status = "Pending" });

            var mockStore = new Mock<ISecureTokenStore>();
            mockStore.Setup(s => s.LoadAsync()).ReturnsAsync(new List<IdentityAccount>());
            mockStore.Setup(s => s.SaveAsync(It.IsAny<List<IdentityAccount>>())).Returns(Task.CompletedTask);

            var logger = new Mock<ILogger<IdentityManager>>();

            var manager = new IdentityManager(new[] { mockStrat.Object }, mockStore.Object, logger.Object);

            // Use the runtime type name of the mock object so FindStrategyForProvider will match
            var providerName = mockStrat.Object.GetType().Name;

            var res = await manager.StartAuth(providerName);

            Assert.NotNull(res);
            mockStrat.Verify(s => s.InitiateFlowAsync(It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task GetToken_ReturnsAccessTokenIfExists()
        {
            var mockStrat = new Mock<IAuthStrategy>();

            var mockStore = new Mock<ISecureTokenStore>();
            mockStore.Setup(s => s.LoadAsync()).ReturnsAsync(new List<IdentityAccount>());
            mockStore.Setup(s => s.SaveAsync(It.IsAny<List<IdentityAccount>>())).Returns(Task.CompletedTask);

            var logger = new Mock<ILogger<IdentityManager>>();

            var manager = new IdentityManager(new[] { mockStrat.Object }, mockStore.Object, logger.Object);

            var acc = new IdentityAccount
            {
                Id = "1",
                Provider = "TestProvider",
                AccessToken = "token-123",
                ExpiresAt = DateTimeOffset.UtcNow.AddHours(1)
            };

            await manager.AddOrUpdateAccountAsync(acc);

            var token = await manager.GetToken("TestProvider");

            Assert.Equal("token-123", token);
        }

        [Fact]
        public async Task GetToken_RefreshesIfExpired()
        {
            var mockStrat = new Mock<IAuthStrategy>();

            var refreshed = new TokenResponse
            {
                AccessToken = "new-token",
                RefreshToken = "new-refresh",
                ExpiresInSeconds = 3600
            };

            mockStrat.Setup(s => s.RefreshTokenAsync(It.IsAny<IdentityAccount>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(refreshed);

            var mockStore = new Mock<ISecureTokenStore>();
            mockStore.Setup(s => s.LoadAsync()).ReturnsAsync(new List<IdentityAccount>());
            mockStore.Setup(s => s.SaveAsync(It.IsAny<List<IdentityAccount>>())).Returns(Task.CompletedTask);

            var logger = new Mock<ILogger<IdentityManager>>();

            var manager = new IdentityManager(new[] { mockStrat.Object }, mockStore.Object, logger.Object);


            var acc = new IdentityAccount
            {
                Id = "1",
                Provider = "TestProvider",
                AccessToken = "old-token",
                RefreshToken = "refresh-1",
                ExpiresAt = DateTimeOffset.UtcNow.AddHours(-1) // expired
            };

            // Ensure the store initially contains the expired account so the background loader populates it
            mockStore.Setup(s => s.LoadAsync()).ReturnsAsync(new List<IdentityAccount> { acc });

            manager = new IdentityManager(new[] { mockStrat.Object }, mockStore.Object, logger.Object);

            // give background load a moment
            await Task.Delay(20);

            var token = await manager.GetToken(acc.Provider);

            Assert.Equal("new-token", token);
            mockStrat.Verify(s => s.RefreshTokenAsync(It.IsAny<IdentityAccount>(), It.IsAny<CancellationToken>()), Times.Once);
            mockStore.Verify(s => s.SaveAsync(It.IsAny<List<IdentityAccount>>()), Times.AtLeastOnce);
        }
    }
}
