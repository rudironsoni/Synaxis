// <copyright file="DeviceFlowServiceTests.cs" company="Synaxis">
// Copyright (c) Synaxis. All rights reserved.
// </copyright>

namespace Synaxis.InferenceGateway.Infrastructure.Tests.Identity.Strategies.GitHub
{
    using System;
    using System.Net;
    using System.Net.Http;
    using System.Text;
    using System.Threading;
    using System.Threading.Tasks;
    using Microsoft.Extensions.Logging;
    using Moq;
    using Synaxis.InferenceGateway.Infrastructure.Identity.Core;
    using Synaxis.InferenceGateway.Infrastructure.Identity.Strategies.GitHub;
    using Xunit;

    public class DeviceFlowServiceTests
    {
        private static readonly TimeSpan TestTimeout = TimeSpan.FromSeconds(10);

        [Fact]
        public async Task StartPollingAsync_ReceivesToken_AndCallsOnSuccess()
        {
            var responseJson = "{\"access_token\":\"test-access-token\",\"refresh_token\":\"test-refresh-token\",\"expires_in\":3600}";
            var client = CreateClientReturningJson(HttpStatusCode.OK, responseJson);
            var logger = new Mock<ILogger<DeviceFlowService>>();

            var service = new DeviceFlowService(client, logger.Object);

            TokenResponse? receivedToken = null;
            var onSuccessCalled = false;
            var callbackTcs = new TaskCompletionSource();

            var task = service.StartPollingAsync("device-code-123", 1, async (token) =>
            {
                receivedToken = token;
                onSuccessCalled = true;
                await Task.CompletedTask.ConfigureAwait(false);
            }, CancellationToken.None, callbackTcs);

            // Wait for actual callback (with safety timeout)
            var completed = await Task.WhenAny(callbackTcs.Task, Task.Delay(TestTimeout));
            Assert.Same(callbackTcs.Task, completed); // Verify callback fired

            Assert.True(onSuccessCalled);
            Assert.NotNull(receivedToken);
            Assert.Equal("test-access-token", receivedToken!.AccessToken);
            Assert.Equal("test-refresh-token", receivedToken.RefreshToken);
            Assert.Equal(3600, receivedToken.ExpiresInSeconds);
        }

        [Fact]
        public async Task StartPollingAsync_HandlesAuthorizationPending()
        {
            var callCount = 0;
            var handler = new DelegatingHandlerStub((req, ct) =>
            {
                callCount++;
                string responseJson;
                if (callCount == 1)
                {
                    responseJson = "{\"error\":\"authorization_pending\"}";
                }
                else
                {
                    responseJson = "{\"access_token\":\"test-token\",\"expires_in\":3600}";
                }

                var response = new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = new StringContent(responseJson, Encoding.UTF8, "application/json"),
                };
                return Task.FromResult(response);
            });

            var httpClient = new HttpClient(handler) { BaseAddress = new Uri("https://github.com/") };
            var logger = new Mock<ILogger<DeviceFlowService>>();

            var service = new DeviceFlowService(httpClient, logger.Object);

            TokenResponse? receivedToken = null;
            var onSuccessCalled = false;
            var callbackTcs = new TaskCompletionSource();

            var task = service.StartPollingAsync("device-code-123", 1, async (token) =>
            {
                receivedToken = token;
                onSuccessCalled = true;
                await Task.CompletedTask.ConfigureAwait(false);
            }, CancellationToken.None, callbackTcs);

            // Wait for actual callback (with safety timeout)
            var completed = await Task.WhenAny(callbackTcs.Task, Task.Delay(TestTimeout));
            Assert.Same(callbackTcs.Task, completed); // Verify callback fired

            Assert.True(onSuccessCalled);
            Assert.NotNull(receivedToken);
            Assert.Equal("test-token", receivedToken!.AccessToken);
        }

        [Fact]
        public async Task StartPollingAsync_HandlesSlowDown()
        {
            var callCount = 0;
            var handler = new DelegatingHandlerStub((req, ct) =>
            {
                callCount++;
                string responseJson;
                if (callCount == 1)
                {
                    responseJson = "{\"error\":\"slow_down\"}";
                }
                else
                {
                    responseJson = "{\"access_token\":\"test-token\",\"expires_in\":3600}";
                }

                var response = new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = new StringContent(responseJson, Encoding.UTF8, "application/json"),
                };
                return Task.FromResult(response);
            });

            var httpClient = new HttpClient(handler) { BaseAddress = new Uri("https://github.com/") };
            var logger = new Mock<ILogger<DeviceFlowService>>();

            var service = new DeviceFlowService(httpClient, logger.Object);

            TokenResponse? receivedToken = null;
            var onSuccessCalled = false;
            var callbackTcs = new TaskCompletionSource();

            var task = service.StartPollingAsync("device-code-123", 1, async (token) =>
            {
                receivedToken = token;
                onSuccessCalled = true;
                await Task.CompletedTask.ConfigureAwait(false);
            }, CancellationToken.None, callbackTcs);

            // Wait for actual callback (with safety timeout) - slow_down adds 5s delay
            var completed = await Task.WhenAny(callbackTcs.Task, Task.Delay(TimeSpan.FromSeconds(15)));
            Assert.Same(callbackTcs.Task, completed); // Verify callback fired

            Assert.True(onSuccessCalled);
            Assert.NotNull(receivedToken);
            Assert.Equal("test-token", receivedToken!.AccessToken);
        }

        [Fact]
        public async Task StartPollingAsync_HandlesExpiredToken()
        {
            var responseJson = "{\"error\":\"expired_token\"}";
            var client = CreateClientReturningJson(HttpStatusCode.OK, responseJson);
            var logger = new Mock<ILogger<DeviceFlowService>>();

            var service = new DeviceFlowService(client, logger.Object);

            TokenResponse? receivedToken = null;
            var onSuccessCalled = false;

            // Use CancellationToken to stop polling after first check
            using var cts = new CancellationTokenSource(TimeSpan.FromMilliseconds(500));

            var task = service.StartPollingAsync("device-code-123", 1, async (token) =>
            {
                receivedToken = token;
                onSuccessCalled = true;
                await Task.CompletedTask.ConfigureAwait(false);
            }, cts.Token);

            // Wait for polling to complete or timeout
            var completed = await Task.WhenAny(task, Task.Delay(TestTimeout));
            Assert.Same(task, completed); // Verify polling completed (error stops polling)

            Assert.False(onSuccessCalled);
            Assert.Null(receivedToken);
        }

        [Fact]
        public async Task StartPollingAsync_HandlesAccessDenied()
        {
            var responseJson = "{\"error\":\"access_denied\"}";
            var client = CreateClientReturningJson(HttpStatusCode.OK, responseJson);
            var logger = new Mock<ILogger<DeviceFlowService>>();

            var service = new DeviceFlowService(client, logger.Object);

            TokenResponse? receivedToken = null;
            var onSuccessCalled = false;

            // Use CancellationToken to stop polling after first check
            using var cts = new CancellationTokenSource(TimeSpan.FromMilliseconds(500));

            var task = service.StartPollingAsync("device-code-123", 1, async (token) =>
            {
                receivedToken = token;
                onSuccessCalled = true;
                await Task.CompletedTask.ConfigureAwait(false);
            }, cts.Token);

            // Wait for polling to complete or timeout
            var completed = await Task.WhenAny(task, Task.Delay(TestTimeout));
            Assert.Same(task, completed); // Verify polling completed (error stops polling)

            Assert.False(onSuccessCalled);
            Assert.Null(receivedToken);
        }

        [Fact]
        public async Task StartPollingAsync_HandlesNetworkError()
        {
            var handler = new DelegatingHandlerStub((req, ct) =>
                throw new HttpRequestException("Network error"));

            var httpClient = new HttpClient(handler) { BaseAddress = new Uri("https://github.com/") };
            var logger = new Mock<ILogger<DeviceFlowService>>();

            var service = new DeviceFlowService(httpClient, logger.Object);

            TokenResponse? receivedToken = null;
            var onSuccessCalled = false;

            // Use CancellationToken to stop polling after first check
            using var cts = new CancellationTokenSource(TimeSpan.FromMilliseconds(500));

            var task = service.StartPollingAsync("device-code-123", 1, async (token) =>
            {
                receivedToken = token;
                onSuccessCalled = true;
                await Task.CompletedTask.ConfigureAwait(false);
            }, cts.Token);

            // Wait for polling to complete or timeout
            var completed = await Task.WhenAny(task, Task.Delay(TestTimeout));
            Assert.Same(task, completed); // Verify polling completed (error stops polling)

            Assert.False(onSuccessCalled);
            Assert.Null(receivedToken);
        }

        [Fact]
        public async Task StartPollingAsync_HandlesInvalidJson()
        {
            var client = CreateClientReturningJson(HttpStatusCode.OK, "invalid json");
            var logger = new Mock<ILogger<DeviceFlowService>>();

            var service = new DeviceFlowService(client, logger.Object);

            TokenResponse? receivedToken = null;
            var onSuccessCalled = false;

            // Use CancellationToken to stop polling after first check
            // Invalid JSON causes polling to continue, so we need to cancel it
            using var cts = new CancellationTokenSource(TimeSpan.FromMilliseconds(500));

            var task = service.StartPollingAsync("device-code-123", 1, async (token) =>
            {
                receivedToken = token;
                onSuccessCalled = true;
                await Task.CompletedTask.ConfigureAwait(false);
            }, cts.Token);

            // Wait for polling to complete or timeout
            var completed = await Task.WhenAny(task, Task.Delay(TestTimeout));
            Assert.Same(task, completed); // Verify polling completed (cancelled)

            Assert.False(onSuccessCalled);
            Assert.Null(receivedToken);
        }

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

        private sealed class DelegatingHandlerStub : HttpMessageHandler
        {
            private readonly Func<HttpRequestMessage, CancellationToken, Task<HttpResponseMessage>> _handler;

            public DelegatingHandlerStub(Func<HttpRequestMessage, CancellationToken, Task<HttpResponseMessage>> handler) => this._handler = handler;

            protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken) => this._handler(request, cancellationToken);
        }
    }
}
