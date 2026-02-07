
namespace Synaxis.InferenceGateway.Infrastructure.Tests.Identity.Strategies.Google
{
    public class GoogleAuthStrategyTests
    {
        private AntigravitySettings CreateSettings() => new AntigravitySettings { ClientId = "cid", ClientSecret = "secret" };
    using Microsoft.Extensions.Logging;
    using Moq;
    using Synaxis.InferenceGateway.Application.Configuration;
    using Synaxis.InferenceGateway.Infrastructure.Identity.Core;
    using Synaxis.InferenceGateway.Infrastructure.Identity.Strategies.Google;
    using System.Collections.Generic;
    using System.Net.Http.Headers;
    using System.Net.Http;
    using System.Net;
    using System.Text.Json;
    using System.Text;
    using System.Threading.Tasks;
    using System.Threading;
    using System;
    using Xunit;

        [Fact]
        public async Task InitiateFlowAsync_GeneratesAuthorizationUrlWithPkce()
        {
            var settings = this.CreateSettings();
            var httpFactory = new Mock<IHttpClientFactory>();
            var logger = new Mock<ILogger<GoogleAuthStrategy>>();

            var strat = new GoogleAuthStrategy(settings, httpFactory.Object, logger.Object);

            var res = await strat.InitiateFlowAsync(CancellationToken.None).ConfigureAwait(false);

            Assert.Equal("Pending", res.Status);
            Assert.NotNull(res.VerificationUri);
            Assert.Contains("accounts.google.com/o/oauth2/v2/auth", res.VerificationUri, StringComparison.Ordinal);
            Assert.Contains("code_challenge=", res.VerificationUri, StringComparison.Ordinal);
            Assert.Contains("code_challenge_method=S256", res.VerificationUri, StringComparison.Ordinal);
            Assert.Contains("client_id=cid", res.VerificationUri, StringComparison.Ordinal);
            Assert.Contains("access_type=offline", res.VerificationUri, StringComparison.Ordinal);
        }

        [Fact]
        public async Task CompleteFlowAsync_ExchangesCode_FetchesUserInfo_EmitsEvent()
        {
            var settings = this.CreateSettings();
            var httpFactory = new Mock<IHttpClientFactory>();
            var logger = new Mock<ILogger<GoogleAuthStrategy>>();

            // Mock HttpMessageHandler by providing custom HttpClient instances for factory
            var tokenPayload = new { access_token = "at-1", refresh_token = "rt-1", expires_in = 3600 };
            var userInfoPayload = new { email = "user@example.com" };
            var projectPayload = new { cloudaicompanionProject = "project-123" };

            var tokenClient = CreateClientReturningJson(HttpStatusCode.OK, JsonSerializer.Serialize(tokenPayload));
            var userClient = CreateClientReturningJson(HttpStatusCode.OK, JsonSerializer.Serialize(userInfoPayload));
            var projectClient = CreateClientReturningJson(HttpStatusCode.OK, JsonSerializer.Serialize(projectPayload));

            var seq = 0;
            httpFactory.Setup(f => f.CreateClient(It.IsAny<string>())).Returns(() =>
            {
                // Return clients in the order used: ExchangeCodeForTokenAsync (Post), FetchUserEmailAsync (Get), FetchProjectIdAsync (Post requests probed repeatedly)
                seq++;
                if (seq == 1)
                {
                    return tokenClient;
                }

                if (seq == 2)
                {
                    return userClient;
                }

                return projectClient;
            });

            var strat = new GoogleAuthStrategy(settings, httpFactory.Object, logger.Object);

            IdentityAccount? emitted = null;
            strat.AccountAuthenticated += (_, acc) => emitted = acc.Account;

            // We need a valid PKCE state value. Call InitiateFlowAsync to get a state value embedded in the URL.
            var init = await strat.InitiateFlowAsync(CancellationToken.None).ConfigureAwait(false);
            // Extract state param from URL
            var uri = new Uri(init.VerificationUri!);
            var query = System.Web.HttpUtility.ParseQueryString(uri.Query);
            var state = query["state"]!;

            var result = await strat.CompleteFlowAsync("auth-code-xyz", state, CancellationToken.None).ConfigureAwait(false);

            Assert.Equal("Completed", result.Status);
            Assert.Equal("user@example.com", result.Message);
            Assert.Equal("project-123", result.UserCode);
            Assert.NotNull(emitted);
            Assert.Equal("google", emitted!.Provider);
            Assert.Equal("user@example.com", emitted.Email);
            Assert.Equal("at-1", emitted.AccessToken);
            Assert.Equal("rt-1", emitted.RefreshToken);
            Assert.True(emitted.Properties.ContainsKey("ProjectId") && emitted.Properties["ProjectId"] == "project-123");
        }

        [Fact]
        public async Task RefreshTokenAsync_RefreshesTokens()
        {
            var settings = this.CreateSettings();
            var httpFactory = new Mock<IHttpClientFactory>();
            var logger = new Mock<ILogger<GoogleAuthStrategy>>();

            var refreshPayload = new { access_token = "new-at", refresh_token = "new-rt", expires_in = 7200 };
            var refreshClient = CreateClientReturningJson(HttpStatusCode.OK, JsonSerializer.Serialize(refreshPayload));
            httpFactory.Setup(f => f.CreateClient(It.IsAny<string>())).Returns(refreshClient);

            var strat = new GoogleAuthStrategy(settings, httpFactory.Object, logger.Object);

            var acc = new IdentityAccount { Provider = "google", Id = "user@example.com", RefreshToken = "old-rt" };
            var tr = await strat.RefreshTokenAsync(acc, CancellationToken.None).ConfigureAwait(false);

            Assert.Equal("new-at", tr.AccessToken);
            Assert.Equal("new-rt", tr.RefreshToken);
            Assert.Equal(7200, tr.ExpiresInSeconds);
        }

        [Fact]
        public async Task RefreshTokenAsync_NetworkFailure_ThrowsInvalidOperationException()
        {
            var settings = this.CreateSettings();
            var httpFactory = new Mock<IHttpClientFactory>();
            var logger = new Mock<ILogger<GoogleAuthStrategy>>();

            var failingClient = CreateClientReturningStatus(HttpStatusCode.InternalServerError, "server err");
            httpFactory.Setup(f => f.CreateClient(It.IsAny<string>())).Returns(failingClient);

            var strat = new GoogleAuthStrategy(settings, httpFactory.Object, logger.Object);

            var acc = new IdentityAccount { Provider = "google", Id = "user@example.com", RefreshToken = "old-rt" };

            await Assert.ThrowsAsync<InvalidOperationException>(() => strat.RefreshTokenAsync(acc, CancellationToken.None)).ConfigureAwait(false);
        }

        [Fact]
        public async Task CompleteFlowAsync_InvalidTokenResponse_HandlesError()
        {
            var settings = this.CreateSettings();
            var httpFactory = new Mock<IHttpClientFactory>();
            var logger = new Mock<ILogger<GoogleAuthStrategy>>();

            // Token endpoint returns invalid payload (missing refresh_token)
            var tokenPayload = new { access_token = "at-1", expires_in = 3600 };
            var tokenClient = CreateClientReturningJson(HttpStatusCode.OK, JsonSerializer.Serialize(tokenPayload));
            httpFactory.Setup(f => f.CreateClient(It.IsAny<string>())).Returns(tokenClient);

            var strat = new GoogleAuthStrategy(settings, httpFactory.Object, logger.Object);

            var init = await strat.InitiateFlowAsync(CancellationToken.None).ConfigureAwait(false);
            var uri = new Uri(init.VerificationUri!);
            var query = System.Web.HttpUtility.ParseQueryString(uri.Query);
            var state = query["state"]!;

            var res = await strat.CompleteFlowAsync("auth-code", state, CancellationToken.None).ConfigureAwait(false);

            Assert.Equal("Error", res.Status);
            Assert.Contains("missing tokens", res.Message, StringComparison.OrdinalIgnoreCase, StringComparison.Ordinal);
        }

        [Fact]
        public async Task FetchUserEmailAsync_InvalidResponse_ReturnsEmptyString()
        {
            var settings = this.CreateSettings();
            var httpFactory = new Mock<IHttpClientFactory>();
            var logger = new Mock<ILogger<GoogleAuthStrategy>>();

            var badClient = CreateClientReturningStatus(HttpStatusCode.Unauthorized, "unauth");
            httpFactory.Setup(f => f.CreateClient(It.IsAny<string>())).Returns(badClient);

            var strat = new GoogleAuthStrategy(settings, httpFactory.Object, logger.Object);

            // Call CompleteFlowAsync path which uses FetchUserEmailAsync; provide a token exchange that returns valid tokens
            var tokenPayload = new { access_token = "at-1", refresh_token = "rt-1", expires_in = 3600 };
            var tokenClient = CreateClientReturningJson(HttpStatusCode.OK, JsonSerializer.Serialize(tokenPayload));

            // Sequence: first call for token exchange -> return tokenClient, then user info -> badClient
            var seq = 0;
            httpFactory.Setup(f => f.CreateClient(It.IsAny<string>())).Returns(() => { seq++; return seq == 1 ? tokenClient : badClient; });

            var init = await strat.InitiateFlowAsync(CancellationToken.None).ConfigureAwait(false);
            var uri = new Uri(init.VerificationUri!);
            var query = System.Web.HttpUtility.ParseQueryString(uri.Query);
            var state = query["state"]!;

            var res = await strat.CompleteFlowAsync("auth-code", state, CancellationToken.None).ConfigureAwait(false);

            Assert.Equal("Completed", res.Status);
            Assert.Null(res.Message);
        }

        // Helpers to create HttpClient instances that return canned responses
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
