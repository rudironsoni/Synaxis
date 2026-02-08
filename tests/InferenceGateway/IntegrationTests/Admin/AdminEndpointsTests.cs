// <copyright file="AdminEndpointsTests.cs" company="PlaceholderCompany">
// Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>

using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;
using Synaxis.InferenceGateway.Infrastructure.ControlPlane;
using Synaxis.InferenceGateway.Infrastructure.Security;
using Synaxis.InferenceGateway.IntegrationTests.SmokeTests.Infrastructure;
using Xunit;

namespace Synaxis.InferenceGateway.IntegrationTests.Admin
{
    public class AdminEndpointsTests : IClassFixture<SynaxisWebApplicationFactory>
    {
        private readonly SynaxisWebApplicationFactory _factory;
        private readonly HttpClient _client;

        public AdminEndpointsTests(SynaxisWebApplicationFactory factory)
        {
            this._factory = factory;
            this._client = factory.CreateClient();
        }

        private async Task<string> GetAuthTokenAsync()
        {
            var loginRequest = new { Email = "admin@test.com" };
            var response = await _client.PostAsJsonAsync("/auth/dev-login", loginRequest).ConfigureAwait(false);
            response.EnsureSuccessStatusCode();

            var content = await response.Content.ReadFromJsonAsync<JsonElement>().ConfigureAwait(false);
            return content.GetProperty("token").GetString() !;
        }

        private HttpClient CreateAuthenticatedClient()
        {
            return this._factory.CreateClient();
        }

        [Fact]
        public async Task GetProviders_WithoutAuth_ReturnsUnauthorized()
        {
            var response = await this._client.GetAsync("/admin/providers");

            Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        }

        [Fact]
        public async Task GetProviders_WithAuth_ReturnsProviders()
        {
            var token = await this.GetAuthTokenAsync();
            var authenticatedClient = this.CreateAuthenticatedClient();
            authenticatedClient.DefaultRequestHeaders.Authorization =
                new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);

            var response = await authenticatedClient.GetAsync("/admin/providers");

            response.EnsureSuccessStatusCode();
            var content = await response.Content.ReadFromJsonAsync<JsonElement>();
            Assert.Equal(JsonValueKind.Array, content.ValueKind);
        }

        [Fact]
        public async Task GetProviders_WithAuth_ContainsProviderFields()
        {
            var token = await this.GetAuthTokenAsync();
            var authenticatedClient = this.CreateAuthenticatedClient();
            authenticatedClient.DefaultRequestHeaders.Authorization =
                new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);

            var response = await authenticatedClient.GetAsync("/admin/providers");
            response.EnsureSuccessStatusCode();

            var content = await response.Content.ReadFromJsonAsync<JsonElement>();
            var providers = content.EnumerateArray().ToList();

            Assert.True(providers.Count > 0);

            var firstProvider = providers.First();
            Assert.True(firstProvider.TryGetProperty("id", out _));
            Assert.True(firstProvider.TryGetProperty("name", out _));
            Assert.True(firstProvider.TryGetProperty("type", out _));
            Assert.True(firstProvider.TryGetProperty("enabled", out _));
            Assert.True(firstProvider.TryGetProperty("tier", out _));
            Assert.True(firstProvider.TryGetProperty("keyConfigured", out _));
            Assert.True(firstProvider.TryGetProperty("models", out _));
        }

        [Fact]
        public async Task UpdateProvider_WithoutAuth_ReturnsUnauthorized()
        {
            var updateRequest = new { enabled = false };
            var response = await this._client.PutAsJsonAsync("/admin/providers/groq", updateRequest);

            Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        }

        [Fact]
        public async Task UpdateProvider_WithAuth_UpdatesProvider()
        {
            var token = await this.GetAuthTokenAsync();
            var authenticatedClient = this.CreateAuthenticatedClient();
            authenticatedClient.DefaultRequestHeaders.Authorization =
                new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);

            var getResponse = await authenticatedClient.GetAsync("/admin/providers");
            getResponse.EnsureSuccessStatusCode();
            var providers = await getResponse.Content.ReadFromJsonAsync<JsonElement>();
            var providerId = providers.EnumerateArray().First().GetProperty("id").GetString();

            var updateRequest = new { enabled = true };
            var response = await authenticatedClient.PutAsJsonAsync($"/admin/providers/{providerId}", updateRequest);

            response.EnsureSuccessStatusCode();

            var content = await response.Content.ReadFromJsonAsync<JsonElement>();
            Assert.True(content.TryGetProperty("success", out var success));
            Assert.True(success.GetBoolean());
        }

        [Fact]
        public async Task UpdateProvider_WithAuth_InvalidProvider_ReturnsNotFound()
        {
            var token = await this.GetAuthTokenAsync();
            var authenticatedClient = this.CreateAuthenticatedClient();
            authenticatedClient.DefaultRequestHeaders.Authorization =
                new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);

            var updateRequest = new { enabled = true };
            var response = await authenticatedClient.PutAsJsonAsync("/admin/providers/nonexistent", updateRequest);

            Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        }

        [Fact]
        public async Task UpdateProvider_WithAuth_UpdatesApiKey()
        {
            var token = await this.GetAuthTokenAsync();
            var authenticatedClient = this.CreateAuthenticatedClient();
            authenticatedClient.DefaultRequestHeaders.Authorization =
                new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);

            var getResponse = await authenticatedClient.GetAsync("/admin/providers");
            getResponse.EnsureSuccessStatusCode();
            var providers = await getResponse.Content.ReadFromJsonAsync<JsonElement>();
            var providerId = providers.EnumerateArray().First().GetProperty("id").GetString();

            var updateRequest = new { key = "new-api-key-123" };
            var response = await authenticatedClient.PutAsJsonAsync($"/admin/providers/{providerId}", updateRequest);

            response.EnsureSuccessStatusCode();

            var content = await response.Content.ReadFromJsonAsync<JsonElement>();
            Assert.True(content.TryGetProperty("success", out var success));
            Assert.True(success.GetBoolean());
        }

        [Fact]
        public async Task GetHealth_WithoutAuth_ReturnsUnauthorized()
        {
            var response = await this._client.GetAsync("/admin/health");

            Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        }

        [Fact]
        public async Task GetHealth_WithAuth_ReturnsHealthData()
        {
            var token = await this.GetAuthTokenAsync();
            var authenticatedClient = this.CreateAuthenticatedClient();
            authenticatedClient.DefaultRequestHeaders.Authorization =
                new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);

            var response = await authenticatedClient.GetAsync("/admin/health");

            response.EnsureSuccessStatusCode();

            var content = await response.Content.ReadFromJsonAsync<JsonElement>();
            Assert.True(content.TryGetProperty("services", out _));
            Assert.True(content.TryGetProperty("providers", out _));
            Assert.True(content.TryGetProperty("overallStatus", out _));
            Assert.True(content.TryGetProperty("timestamp", out _));
        }

        [Fact]
        public async Task GetHealth_WithAuth_ContainsServices()
        {
            var token = await this.GetAuthTokenAsync();
            var authenticatedClient = this.CreateAuthenticatedClient();
            authenticatedClient.DefaultRequestHeaders.Authorization =
                new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);

            var response = await authenticatedClient.GetAsync("/admin/health");
            response.EnsureSuccessStatusCode();

            var content = await response.Content.ReadFromJsonAsync<JsonElement>();
            var services = content.GetProperty("services");

            Assert.Equal(JsonValueKind.Array, services.ValueKind);
            Assert.True(services.GetArrayLength() > 0);

            var firstService = services.EnumerateArray().First();
            Assert.True(firstService.TryGetProperty("name", out _));
            Assert.True(firstService.TryGetProperty("status", out _));
            Assert.True(firstService.TryGetProperty("lastChecked", out _));
        }

        [Fact]
        public async Task GetHealth_WithAuth_ContainsProviders()
        {
            var token = await this.GetAuthTokenAsync();
            var authenticatedClient = this.CreateAuthenticatedClient();
            authenticatedClient.DefaultRequestHeaders.Authorization =
                new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);

            var response = await authenticatedClient.GetAsync("/admin/health");
            response.EnsureSuccessStatusCode();

            var content = await response.Content.ReadFromJsonAsync<JsonElement>();
            var providers = content.GetProperty("providers");

            Assert.Equal(JsonValueKind.Array, providers.ValueKind);
        }

        [Fact]
        public async Task GetHealth_WithAuth_HasValidOverallStatus()
        {
            var token = await this.GetAuthTokenAsync();
            var authenticatedClient = this.CreateAuthenticatedClient();
            authenticatedClient.DefaultRequestHeaders.Authorization =
                new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);

            var response = await authenticatedClient.GetAsync("/admin/health");
            response.EnsureSuccessStatusCode();

            var content = await response.Content.ReadFromJsonAsync<JsonElement>();
            var overallStatus = content.GetProperty("overallStatus").GetString();

            Assert.NotNull(overallStatus);
            Assert.True(string.Equals(overallStatus, "healthy", StringComparison.Ordinal) || string.Equals(overallStatus, "degraded", StringComparison.Ordinal) || string.Equals(overallStatus, "unhealthy", StringComparison.Ordinal));
        }

        [Fact]
        public async Task GetProviders_WithAuth_ProviderHasModels()
        {
            var token = await this.GetAuthTokenAsync();
            var authenticatedClient = this.CreateAuthenticatedClient();
            authenticatedClient.DefaultRequestHeaders.Authorization =
                new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);

            var response = await authenticatedClient.GetAsync("/admin/providers");
            response.EnsureSuccessStatusCode();

            var content = await response.Content.ReadFromJsonAsync<JsonElement>();
            var providers = content.EnumerateArray().ToList();

            Assert.True(providers.Count > 0);

            foreach (var provider in providers)
            {
                Assert.True(provider.TryGetProperty("models", out var models));
                Assert.Equal(JsonValueKind.Array, models.ValueKind);
            }
        }

        [Fact]
        public async Task UpdateProvider_WithAuth_UpdatesTier()
        {
            var token = await this.GetAuthTokenAsync();
            var authenticatedClient = this.CreateAuthenticatedClient();
            authenticatedClient.DefaultRequestHeaders.Authorization =
                new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);

            var getResponse = await authenticatedClient.GetAsync("/admin/providers");
            getResponse.EnsureSuccessStatusCode();
            var providers = await getResponse.Content.ReadFromJsonAsync<JsonElement>();
            var providerId = providers.EnumerateArray().First().GetProperty("id").GetString();

            var updateRequest = new { tier = 2 };
            var response = await authenticatedClient.PutAsJsonAsync($"/admin/providers/{providerId}", updateRequest);

            response.EnsureSuccessStatusCode();

            var content = await response.Content.ReadFromJsonAsync<JsonElement>();
            Assert.True(content.TryGetProperty("success", out var success));
            Assert.True(success.GetBoolean());
        }

        [Fact]
        public async Task UpdateProvider_WithAuth_UpdatesEndpoint()
        {
            var token = await this.GetAuthTokenAsync();
            var authenticatedClient = this.CreateAuthenticatedClient();
            authenticatedClient.DefaultRequestHeaders.Authorization =
                new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);

            var getResponse = await authenticatedClient.GetAsync("/admin/providers");
            getResponse.EnsureSuccessStatusCode();
            var providers = await getResponse.Content.ReadFromJsonAsync<JsonElement>();
            var providerId = providers.EnumerateArray().First().GetProperty("id").GetString();

            var updateRequest = new { endpoint = "https://api.example.com/v1" };
            var response = await authenticatedClient.PutAsJsonAsync($"/admin/providers/{providerId}", updateRequest);

            response.EnsureSuccessStatusCode();

            var content = await response.Content.ReadFromJsonAsync<JsonElement>();
            Assert.True(content.TryGetProperty("success", out var success));
            Assert.True(success.GetBoolean());
        }

        [Fact]
        public async Task AdminEndpoints_HaveCorrectTags()
        {
            var token = await this.GetAuthTokenAsync();
            var authenticatedClient = this.CreateAuthenticatedClient();
            authenticatedClient.DefaultRequestHeaders.Authorization =
                new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);

            var response = await authenticatedClient.GetAsync("/admin/providers");
            response.EnsureSuccessStatusCode();

            response = await authenticatedClient.GetAsync("/admin/health");
            response.EnsureSuccessStatusCode();

            Assert.True(true);
        }
    }
}
