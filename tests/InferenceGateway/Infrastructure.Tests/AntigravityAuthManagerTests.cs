// <copyright file="AntigravityAuthManagerTests.cs" company="Synaxis">
// Copyright (c) Synaxis. All rights reserved.
// </copyright>

namespace Synaxis.InferenceGateway.Infrastructure.Tests;

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.Extensions.Http;
using Microsoft.Extensions.Logging;
using Moq;
using Synaxis.InferenceGateway.Application.Configuration;
using Synaxis.InferenceGateway.Infrastructure.Auth;
using Xunit;

public sealed class AntigravityAuthManagerTests : IDisposable
{
    private readonly string _tempAuthPath;
    private readonly Mock<ILogger<AntigravityAuthManager>> _loggerMock;

    public AntigravityAuthManagerTests()
    {
        this._tempAuthPath = Path.GetTempFileName();
        this._loggerMock = new Mock<ILogger<AntigravityAuthManager>>();
    }

    public void Dispose()
    {
        if (File.Exists(this._tempAuthPath))
        {
            File.Delete(this._tempAuthPath);
        }
    }

    [Fact]
    public async Task ListAccounts_ReturnsLoadedAccounts()
    {
        // Arrange
        var accounts = new List<AntigravityAccount>
        {
            new() { Email = "user1@test.com", Token = new() { AccessToken = "token1", ExpiresInSeconds = 3600, IssuedUtc = DateTime.UtcNow } },
            new() { Email = "user2@test.com", Token = new() { AccessToken = "token2", ExpiresInSeconds = 3600, IssuedUtc = DateTime.UtcNow } },
        };
        await File.WriteAllTextAsync(this._tempAuthPath, System.Text.Json.JsonSerializer.Serialize(accounts));
        var httpClientFactory = CreateHttpClientFactory(() => new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent("{}"),
        });
        var settings = new AntigravitySettings { ClientId = "test-client", ClientSecret = "test-secret" };
        var manager = new AntigravityAuthManager("proj", this._tempAuthPath, settings, this._loggerMock.Object, httpClientFactory);

        // Act
        // Force load by calling GetTokenAsync.
        // Since tokens are valid, it should just return the first one and not hit Google.
        var token = await manager.GetTokenAsync();
        var list = manager.ListAccounts().ToList();

        // Assert
        // The manager implements round robin logic.
        // We verify that accounts are loaded correctly from disk.
        Assert.Equal(2, list.Count);
        Assert.Contains(list, a => string.Equals(a.Email, "user1@test.com", StringComparison.Ordinal));
        Assert.Contains(list, a => string.Equals(a.Email, "user2@test.com", StringComparison.Ordinal));
    }

    [Fact]
    public async Task GetTokenAsync_InjectsEnvVarToken()
    {
        // Arrange
        Environment.SetEnvironmentVariable("ANTIGRAVITY_REFRESH_TOKEN", "env-token");
        try
        {
            // Empty file
            await File.WriteAllTextAsync(this._tempAuthPath, "[]");
            var httpClientFactory = CreateHttpClientFactory(() => new HttpResponseMessage(HttpStatusCode.BadRequest)
            {
                Content = new StringContent("{\"error\":\"invalid_grant\"}"),
            });
            var settings = new AntigravitySettings { ClientId = "test-client", ClientSecret = "test-secret" };
            var manager = new AntigravityAuthManager("proj", this._tempAuthPath, settings, this._loggerMock.Object, httpClientFactory);

            // Act
            // GetTokenAsync will detect env var, inject it.
            // But the token is expired (ExpiresInSeconds=0, IssuedUtc=-1h).
            // So it will try to Refresh.
            // This will throw because we can't refresh against real Google API without a valid refresh token and network.
            // So we expect an Exception, but we verify the account was added to the list.
            await Assert.ThrowsAnyAsync<Exception>(() => manager.GetTokenAsync());

            var list = manager.ListAccounts().ToList();
            Assert.Contains(list, a => string.Equals(a.Email, "env-var-user@system", StringComparison.Ordinal));
        }
        finally
        {
            Environment.SetEnvironmentVariable("ANTIGRAVITY_REFRESH_TOKEN", null);
        }
    }

    private static IHttpClientFactory CreateHttpClientFactory(Func<HttpResponseMessage> responseFactory)
    {
        var handler = new StubHttpMessageHandler(responseFactory);
        var client = new HttpClient(handler);
        var factoryMock = new Mock<IHttpClientFactory>();
        factoryMock.Setup(x => x.CreateClient(It.IsAny<string>())).Returns(client);
        return factoryMock.Object;
    }

    private sealed class StubHttpMessageHandler : HttpMessageHandler
    {
        private readonly Func<HttpResponseMessage> _responseFactory;

        public StubHttpMessageHandler(Func<HttpResponseMessage> responseFactory)
        {
            this._responseFactory = responseFactory;
        }

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, System.Threading.CancellationToken cancellationToken)
        {
            return Task.FromResult(this._responseFactory());
        }
    }
}
