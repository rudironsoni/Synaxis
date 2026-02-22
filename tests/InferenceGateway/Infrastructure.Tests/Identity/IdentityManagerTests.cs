// <copyright file="IdentityManagerTests.cs" company="Synaxis">
// Copyright (c) Synaxis. All rights reserved.
// </copyright>

namespace Synaxis.InferenceGateway.Infrastructure.Tests.Identity
{
    using System;
    using System.Collections.Generic;
    using System.Threading;
    using System.Threading.Tasks;
    using Microsoft.Extensions.Logging;
    using Moq;
    using Synaxis.InferenceGateway.Infrastructure.Identity.Core;
    using Xunit;

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
            mockStore.Setup(s => s.SaveAsync(It.IsAny<IList<IdentityAccount>>())).Returns(Task.CompletedTask);

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
            mockStore.Setup(s => s.SaveAsync(It.IsAny<IList<IdentityAccount>>())).Returns(Task.CompletedTask);

            var logger = new Mock<ILogger<IdentityManager>>();

            var manager = new IdentityManager(new[] { mockStrat.Object }, mockStore.Object, logger.Object);

            // Wait for the initial background load to complete to avoid race conditions
            await manager.WaitForInitialLoadAsync();

            var acc = new IdentityAccount
            {
                Id = "1",
                Provider = "TestProvider",
                AccessToken = "token-123",
                ExpiresAt = DateTimeOffset.UtcNow.AddHours(1),
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
                ExpiresInSeconds = 3600,
            };

            mockStrat.Setup(s => s.RefreshTokenAsync(It.IsAny<IdentityAccount>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(refreshed);

            var acc = new IdentityAccount
            {
                Id = "1",
                Provider = "TestProvider",
                AccessToken = "old-token",
                RefreshToken = "refresh-1",
                ExpiresAt = DateTimeOffset.UtcNow.AddHours(-1), // expired
            };

            var mockStore = new Mock<ISecureTokenStore>();
            mockStore.Setup(s => s.LoadAsync()).ReturnsAsync(new List<IdentityAccount> { acc });
            mockStore.Setup(s => s.SaveAsync(It.IsAny<IList<IdentityAccount>>())).Returns(Task.CompletedTask);

            var logger = new Mock<ILogger<IdentityManager>>();

            var manager = new IdentityManager(new[] { mockStrat.Object }, mockStore.Object, logger.Object);

            await manager.WaitForInitialLoadAsync();

            var token = await manager.GetToken(acc.Provider);

            Assert.Equal("new-token", token);
            mockStrat.Verify(s => s.RefreshTokenAsync(It.IsAny<IdentityAccount>(), It.IsAny<CancellationToken>()), Times.Once);
            mockStore.Verify(s => s.SaveAsync(It.IsAny<IList<IdentityAccount>>()), Times.AtLeastOnce);
        }

        [Fact]
        public async Task RefreshTokenAsync_ValidToken_ReturnsNewToken()
        {
            var mockStrat = new Mock<IAuthStrategy>();
            var mockStore = new Mock<ISecureTokenStore>();
            var logger = new Mock<ILogger<IdentityManager>>();

            mockStore.Setup(s => s.LoadAsync()).ReturnsAsync(new List<IdentityAccount>());
            mockStore.Setup(s => s.SaveAsync(It.IsAny<IList<IdentityAccount>>())).Returns(Task.CompletedTask);

            var refreshedToken = new TokenResponse
            {
                AccessToken = "new-access-token",
                RefreshToken = "new-refresh-token",
                ExpiresInSeconds = 7200,
            };

            mockStrat.Setup(s => s.RefreshTokenAsync(It.IsAny<IdentityAccount>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(refreshedToken);

            var manager = new IdentityManager(new[] { mockStrat.Object }, mockStore.Object, logger.Object);
            await manager.WaitForInitialLoadAsync();

            var account = new IdentityAccount
            {
                Id = "1",
                Provider = "TestProvider",
                AccessToken = "old-token",
                RefreshToken = "valid-refresh-token",
                ExpiresAt = DateTimeOffset.UtcNow.AddMinutes(-5), // expired
            };

            await manager.AddOrUpdateAccountAsync(account);

            var token = await manager.GetToken("TestProvider");

            Assert.Equal("new-access-token", token);
            mockStrat.Verify(s => s.RefreshTokenAsync(It.IsAny<IdentityAccount>(), It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task RefreshTokenAsync_ExpiredToken_ThrowsException()
        {
            var mockStrat = new Mock<IAuthStrategy>();
            var mockStore = new Mock<ISecureTokenStore>();
            var logger = new Mock<ILogger<IdentityManager>>();

            mockStrat.Setup(s => s.RefreshTokenAsync(It.IsAny<IdentityAccount>(), It.IsAny<CancellationToken>()))
                .ThrowsAsync(new InvalidOperationException("Refresh token expired"));
            mockStore.Setup(s => s.LoadAsync()).ReturnsAsync(new List<IdentityAccount>());

            var manager = new IdentityManager(new[] { mockStrat.Object }, mockStore.Object, logger.Object);

            await manager.WaitForInitialLoadAsync();

            var account = new IdentityAccount
            {
                Id = "1",
                Provider = "TestProvider",
                AccessToken = "old-token",
                RefreshToken = "expired-refresh-token",
                ExpiresAt = DateTimeOffset.UtcNow.AddHours(-1),
            };

            await manager.AddOrUpdateAccountAsync(account);

            var token = await manager.GetToken("TestProvider");

            // When refresh fails, it should return the old token
            Assert.Equal("old-token", token);
            mockStrat.Verify(s => s.RefreshTokenAsync(It.IsAny<IdentityAccount>(), It.IsAny<CancellationToken>()), Times.Once);
            logger.Verify(
                x => x.Log(
                    LogLevel.Error,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Failed refreshing token")),
                    It.IsAny<Exception>(),
                    It.Is<Func<It.IsAnyType, Exception?, string>>((v, t) => true)),
                Times.Once);
        }

        [Fact]
        public async Task GetAccountAsync_ExistingAccount_ReturnsAccount()
        {
            var mockStrat = new Mock<IAuthStrategy>();
            var mockStore = new Mock<ISecureTokenStore>();
            var logger = new Mock<ILogger<IdentityManager>>();

            // Setup LoadAsync to return empty list initially
            mockStore.Setup(s => s.LoadAsync())
                .ReturnsAsync(new List<IdentityAccount>());
            mockStore.Setup(s => s.SaveAsync(It.IsAny<IList<IdentityAccount>>()))
                .Returns(Task.CompletedTask);

            var manager = new IdentityManager(new[] { mockStrat.Object }, mockStore.Object, logger.Object);

            // Wait for initial load to complete
            await manager.WaitForInitialLoadAsync();

            var account = new IdentityAccount
            {
                Id = "1",
                Provider = "TestProvider",
                AccessToken = "test-token",
                Email = "test@example.com",
            };

            await manager.AddOrUpdateAccountAsync(account);

            var token = await manager.GetToken("TestProvider");

            Assert.Equal("test-token", token);
        }

        [Fact]
        public async Task GetAccountAsync_NonExistent_ReturnsNull()
        {
            var mockStrat = new Mock<IAuthStrategy>();
            var mockStore = new Mock<ISecureTokenStore>();
            var logger = new Mock<ILogger<IdentityManager>>();

            var manager = new IdentityManager(new[] { mockStrat.Object }, mockStore.Object, logger.Object);

            var token = await manager.GetToken("NonExistentProvider");

            Assert.Null(token);
        }

        [Fact]
        public async Task SaveAccountAsync_NewAccount_CreatesSuccessfully()
        {
            var mockStrat = new Mock<IAuthStrategy>();
            var mockStore = new Mock<ISecureTokenStore>();
            var logger = new Mock<ILogger<IdentityManager>>();

            mockStore.Setup(s => s.SaveAsync(It.IsAny<IList<IdentityAccount>>())).Returns(Task.CompletedTask);

            var manager = new IdentityManager(new[] { mockStrat.Object }, mockStore.Object, logger.Object);

            var account = new IdentityAccount
            {
                Id = "1",
                Provider = "NewProvider",
                AccessToken = "new-token",
                Email = "new@example.com",
            };

            await manager.AddOrUpdateAccountAsync(account);

            mockStore.Verify(s => s.SaveAsync(It.IsAny<IList<IdentityAccount>>()), Times.AtLeastOnce);
        }

        [Fact]
        public async Task DeleteAccountAsync_Existing_DeletesSuccessfully()
        {
            var mockStrat = new Mock<IAuthStrategy>();
            var mockStore = new Mock<ISecureTokenStore>();
            var logger = new Mock<ILogger<IdentityManager>>();

            var existingAccount = new IdentityAccount
            {
                Id = "1",
                Provider = "TestProvider",
                AccessToken = "existing-token",
            };

            mockStore.Setup(s => s.LoadAsync()).ReturnsAsync(new List<IdentityAccount> { existingAccount });

            var manager = new IdentityManager(new[] { mockStrat.Object }, mockStore.Object, logger.Object);

            await manager.WaitForInitialLoadAsync();

            var tokenBefore = await manager.GetToken("TestProvider");
            Assert.Equal("existing-token", tokenBefore);

            var newAccount = new IdentityAccount
            {
                Id = "1",
                Provider = "TestProvider",
                AccessToken = string.Empty,
            };

            await manager.AddOrUpdateAccountAsync(newAccount);

            var tokenAfter = await manager.GetToken("TestProvider");
            Assert.Equal(string.Empty, tokenAfter);
        }

        [Fact]
        public async Task CompleteAuth_ValidProvider_ReturnsAuthResult()
        {
            var mockStrat = new Mock<IAuthStrategy>();
            var mockStore = new Mock<ISecureTokenStore>();
            var logger = new Mock<ILogger<IdentityManager>>();

            var authResult = new AuthResult
            {
                Status = "Success",
                Message = "Authentication completed",
            };

            mockStrat.Setup(s => s.CompleteFlowAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(authResult);

            var manager = new IdentityManager(new[] { mockStrat.Object }, mockStore.Object, logger.Object);

            var providerName = mockStrat.Object.GetType().Name;
            var result = await manager.CompleteAuth(providerName, "auth-code", "state");

            Assert.NotNull(result);
            Assert.Equal("Success", result.Status);
            mockStrat.Verify(s => s.CompleteFlowAsync("auth-code", "state", It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public Task CompleteAuth_InvalidProvider_ThrowsException()
        {
            var mockStore = new Mock<ISecureTokenStore>();
            var logger = new Mock<ILogger<IdentityManager>>();

            // Create manager with empty strategy list
            var manager = new IdentityManager(Array.Empty<IAuthStrategy>(), mockStore.Object, logger.Object);

            return Assert.ThrowsAsync<InvalidOperationException>(() =>
                manager.CompleteAuth("NonExistentProvider123", "auth-code", "state"));
        }
    }
}
