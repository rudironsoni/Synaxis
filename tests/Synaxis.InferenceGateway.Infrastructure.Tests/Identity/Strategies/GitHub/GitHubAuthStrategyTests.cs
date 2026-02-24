// <copyright file="GitHubAuthStrategyTests.cs" company="Synaxis">
// Copyright (c) Synaxis. All rights reserved.
// </copyright>

namespace Synaxis.InferenceGateway.Infrastructure.Tests.Identity.Strategies.GitHub
{
    using System;
    using System.IO;
    using System.Net;
    using System.Net.Http;
    using System.Text;
    using System.Text.Json;
    using System.Threading;
    using System.Threading.Tasks;
    using Microsoft.Extensions.Logging;
    using Moq;
    using Synaxis.InferenceGateway.Infrastructure.Identity.Core;
    using Synaxis.InferenceGateway.Infrastructure.Identity.Strategies.GitHub;
    using Xunit;

    [Collection("GitHubAuthTests")]
    public sealed class GitHubAuthStrategyTests : IDisposable
    {
        private readonly string? _origHome;

        public GitHubAuthStrategyTests()
        {
            // Preserve HOME so tests can safely change it when GhConfigWriter writes files
            this._origHome = Environment.GetEnvironmentVariable("HOME");
        }

        public void Dispose()
        {
            // restore HOME
            if (this._origHome is null)
            {
                Environment.SetEnvironmentVariable("HOME", null);
            }
            else
            {
                Environment.SetEnvironmentVariable("HOME", this._origHome);
            }
        }

        [Fact]
        public async Task InitiateFlowAsync_GeneratesDeviceCodeAndStartsPolling()
        {
            var devicePayload = new { device_code = "dev-123", user_code = "USER-CODE", verification_uri = "https://github.com/login/device" };
            var deviceClient = CreateClientReturningJson(HttpStatusCode.OK, JsonSerializer.Serialize(devicePayload));

            // polling client will immediately return a token so StartPollingAsync will call back
            var pollPayload = new { access_token = "at-1", refresh_token = "rt-1", expires_in = 3600 };
            var pollingClient = CreateClientReturningJson(HttpStatusCode.OK, JsonSerializer.Serialize(pollPayload));

            var deviceLogger = new Mock<ILogger<DeviceFlowService>>();
            var deviceService = new DeviceFlowService(pollingClient, deviceLogger.Object);

            var logger = new Mock<ILogger<GitHubAuthStrategy>>();
            var strat = new GitHubAuthStrategy(deviceClient, deviceService, logger.Object);

            var tcs = new TaskCompletionSource<IdentityAccount>();
            strat.AccountAuthenticated += (_, acc) => tcs.TrySetResult(acc.Account);

            var res = await strat.InitiateFlowAsync(CancellationToken.None);

            Assert.Equal("Pending", res.Status);
            Assert.Equal(devicePayload.user_code, res.UserCode);
            Assert.Equal(devicePayload.verification_uri, res.VerificationUri);

            // Wait for background polling to complete and invoke callback
            var emitted = await tcs.Task;
            Assert.NotNull(emitted);
            Assert.Equal("github", emitted.Provider);
        }

        [Fact]
        public async Task InitiateFlowAsync_InvalidResponse_ReturnsError()
        {
            var client = CreateClientReturningStatus(HttpStatusCode.OK, "not-a-json");
            var mockDevice = new Mock<DeviceFlowService>(client, Mock.Of<ILogger<DeviceFlowService>>());
            var logger = new Mock<ILogger<GitHubAuthStrategy>>();

            var strat = new GitHubAuthStrategy(client, mockDevice.Object, logger.Object);

            var res = await strat.InitiateFlowAsync(CancellationToken.None);

            Assert.Equal("Error", res.Status);
            Assert.NotNull(res.Message);
        }

        [Fact]
        public async Task RefreshTokenAsync_RefreshesTokens()
        {
            var payload = new { access_token = "at-1", refresh_token = "rt-1", expires_in = 3600 };
            var client = CreateClientReturningJson(HttpStatusCode.OK, JsonSerializer.Serialize(payload));
            var mockDevice = new Mock<DeviceFlowService>(client, Mock.Of<ILogger<DeviceFlowService>>());
            var logger = new Mock<ILogger<GitHubAuthStrategy>>();

            var strat = new GitHubAuthStrategy(client, mockDevice.Object, logger.Object);

            var account = new IdentityAccount { Provider = "github", Id = "1", RefreshToken = "old-rt" };
            var tr = await strat.RefreshTokenAsync(account, CancellationToken.None);

            Assert.Equal("at-1", tr.AccessToken);
            Assert.Equal("rt-1", tr.RefreshToken);
            Assert.Equal(3600, tr.ExpiresInSeconds);
        }

        [Fact]
        public Task RefreshTokenAsync_NetworkFailure_Throws()
        {
            // Handler that throws to simulate network failure
            var handler = new DelegatingHandlerStub((req, ct) => throw new HttpRequestException("network"));
            var client = new HttpClient(handler) { BaseAddress = new Uri("https://example.com") };
            var mockDevice = new Mock<DeviceFlowService>(client, Mock.Of<ILogger<DeviceFlowService>>());
            var logger = new Mock<ILogger<GitHubAuthStrategy>>();

            var strat = new GitHubAuthStrategy(client, mockDevice.Object, logger.Object);

            var account = new IdentityAccount { Provider = "github", Id = "1", RefreshToken = "rt" };

            return Assert.ThrowsAsync<HttpRequestException>(() => strat.RefreshTokenAsync(account, CancellationToken.None));
        }

        [Fact]
        public async Task DeviceFlowCallback_EmitsAccountAuthenticatedAndWritesGhConfig()
        {
            // Use temp HOME so GhConfigWriter writes to a safe location
            var tmp = Path.Combine(Path.GetTempPath(), "gh-test-" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(tmp);
            Environment.SetEnvironmentVariable("HOME", tmp);

            var devicePayload = new { device_code = "dev-abc", user_code = "U-CODE", verification_uri = "https://github.com/login/device" };
            var client = CreateClientReturningJson(HttpStatusCode.OK, JsonSerializer.Serialize(devicePayload));

            var mockDevice = new Mock<DeviceFlowService>(client, Mock.Of<ILogger<DeviceFlowService>>());

            Func<TokenResponse, Task>? capturedCallback = null;
            mockDevice.Setup(d => d.StartPollingAsync(It.IsAny<string>(), It.IsAny<int>(), It.IsAny<Func<TokenResponse, Task>>(), It.IsAny<CancellationToken>(), It.IsAny<TaskCompletionSource>()))
                .Callback<string, int, Func<TokenResponse, Task>, CancellationToken, TaskCompletionSource>((dc, iv, cb, ct, tcs) => capturedCallback = cb)
                .Returns(Task.CompletedTask);

            var logger = new Mock<ILogger<GitHubAuthStrategy>>();
            var strat = new GitHubAuthStrategy(client, mockDevice.Object, logger.Object);

            IdentityAccount? emitted = null;
            strat.AccountAuthenticated += (_, acc) => emitted = acc.Account;

            var res = await strat.InitiateFlowAsync(CancellationToken.None);
            Assert.Equal("Pending", res.Status);

            Assert.NotNull(capturedCallback);

            // Simulate device flow returning a token
            var token = new TokenResponse { AccessToken = "access-1", RefreshToken = "refresh-1", ExpiresInSeconds = 3600 };
            await capturedCallback!(token);

            // AccountAuthenticated should have been invoked
            Assert.NotNull(emitted);
            Assert.Equal("github", emitted!.Provider);
            Assert.Equal("access-1", emitted.AccessToken);
            Assert.Equal("refresh-1", emitted.RefreshToken);

            // Ensure GH config file was written to the temp HOME
            var cfgPath = Path.Combine(tmp, ".config", "gh", "hosts.yml");
            Assert.True(File.Exists(cfgPath));
            var contents = await File.ReadAllTextAsync(cfgPath);
            Assert.Contains("oauth_token: access-1", contents, StringComparison.Ordinal);

            // cleanup
            try
            {
                Directory.Delete(tmp, true);
            }
            catch
            {
            }
        }

        // Helpers
        private static HttpClient CreateClientReturningJson(HttpStatusCode status, string json)
        {
            var handler = new DelegatingHandlerStub((req, ct) =>
            {
                var resp = new HttpResponseMessage(status)
                {
                    Content = new StringContent(json, Encoding.UTF8, "application/json"),
                };
                return Task.FromResult(resp);
            });
            return new HttpClient(handler) { BaseAddress = new Uri("https://example.com") };
        }

        private static HttpClient CreateClientReturningStatus(HttpStatusCode status, string body)
        {
            var handler = new DelegatingHandlerStub((req, ct) =>
            {
                var resp = new HttpResponseMessage(status)
                {
                    Content = new StringContent(body, Encoding.UTF8, "text/plain"),
                };
                return Task.FromResult(resp);
            });
            return new HttpClient(handler) { BaseAddress = new Uri("https://example.com") };
        }

        private sealed class DelegatingHandlerStub : HttpMessageHandler
        {
            private readonly Func<HttpRequestMessage, CancellationToken, Task<HttpResponseMessage>> _handler;

            public DelegatingHandlerStub(Func<HttpRequestMessage, CancellationToken, Task<HttpResponseMessage>> handler) => this._handler = handler;

            protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken) => this._handler(request, cancellationToken);
        }
    }
}
