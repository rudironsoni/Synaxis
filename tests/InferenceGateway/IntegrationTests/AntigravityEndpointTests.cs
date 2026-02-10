// <copyright file="AntigravityEndpointTests.cs" company="PlaceholderCompany">
// Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>

namespace Synaxis.InferenceGateway.IntegrationTests
{
    using System;
    using System.Collections.Generic;
    using System.Net;
    using System.Net.Http.Json;
    using System.Text.Json;
    using System.Threading.Tasks;
    using Microsoft.AspNetCore.TestHost;
    using Microsoft.Extensions.Configuration;
    using Microsoft.Extensions.DependencyInjection;
    using Moq;
    using Synaxis.InferenceGateway.Infrastructure.Auth;
    using Xunit;
    using Xunit.Abstractions;

    [Collection("Integration")]
    public class AntigravityEndpointTests : IClassFixture<SynaxisWebApplicationFactory>
    {
        private readonly SynaxisWebApplicationFactory _factory;
        private readonly HttpClient _client;

        public AntigravityEndpointTests(SynaxisWebApplicationFactory factory, ITestOutputHelper output)
        {
            this._factory = factory;
            this._factory.OutputHelper = output;

            // We need to override the IAntigravityAuthManager to avoid real file I/O or Google calls during integration tests
            this._client = this._factory.WithWebHostBuilder(builder =>
            {
                builder.ConfigureTestServices(services =>
                {
                    var mockAuth = new Mock<IAntigravityAuthManager>();
                    mockAuth.Setup(x => x.ListAccounts()).Returns(new List<AccountInfo>
                    {
                    new AccountInfo("test@example.com", true),
                    });
                    mockAuth.Setup(x => x.StartAuthFlow(It.IsAny<string>())).Returns("https://accounts.google.com/o/oauth2/auth?mock=true");
                    mockAuth.Setup(x => x.CompleteAuthFlowAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string?>())).Returns(Task.CompletedTask);

                    services.AddSingleton<IAntigravityAuthManager>(mockAuth.Object);
                });
            }).CreateClient();
        }

        [Fact]
        public async Task Get_Accounts_ReturnsList()
        {
            var response = await this._client.GetAsync("/antigravity/accounts");
            response.EnsureSuccessStatusCode();

            var accounts = await response.Content.ReadFromJsonAsync<List<AccountInfo>>();
            Assert.NotNull(accounts);
            Assert.Single(accounts);
            Assert.Equal("test@example.com", accounts[0].Email, StringComparer.Ordinal);
        }

        [Fact]
        public async Task Post_StartAuth_ReturnsUrl()
        {
            var request = new { RedirectUrl = "http://localhost/cb" };
            var response = await this._client.PostAsJsonAsync("/antigravity/auth/start", request);
            response.EnsureSuccessStatusCode();

            var result = await response.Content.ReadFromJsonAsync<JsonElement>();
            Assert.Equal(
                "https://accounts.google.com/o/oauth2/auth?mock=true",
                result.GetProperty("authUrl").GetString(),
                StringComparer.Ordinal);
        }

        [Fact]
        public async Task Post_CompleteAuth_ReturnsSuccess()
        {
            var request = new { Code = "123", State = "state-abc", RedirectUrl = "http://localhost/cb" };
            var response = await this._client.PostAsJsonAsync("/antigravity/auth/complete", request);
            response.EnsureSuccessStatusCode();

            var result = await response.Content.ReadFromJsonAsync<JsonElement>();
            Assert.Equal(
                "Authentication successful. Account added.",
                result.GetProperty("message").GetString(),
                StringComparer.Ordinal);
        }
    }
}
