// <copyright file="ProvidersEndpointTests.cs" company="PlaceholderCompany">
// Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>

using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Synaxis.InferenceGateway.IntegrationTests.SmokeTests.Infrastructure;
using Xunit;

namespace Synaxis.InferenceGateway.IntegrationTests.Dashboard
{
    public class ProvidersEndpointTests : IClassFixture<SynaxisWebApplicationFactory>
    {
        private readonly SynaxisWebApplicationFactory _factory;
        private readonly HttpClient _client;

        public ProvidersEndpointTests(SynaxisWebApplicationFactory factory)
        {
            this._factory = factory;
            this._client = factory.CreateClient();
        }

        [Fact]
        public async Task GetProviders_ReturnsProvidersList()
        {
            var response = await this._client.GetAsync("/api/providers");

            response.EnsureSuccessStatusCode();
            var content = await response.Content.ReadFromJsonAsync<JsonElement>();

            Assert.True(content.TryGetProperty("providers", out var providers));
            Assert.Equal(JsonValueKind.Array, providers.ValueKind);
        }

        [Fact]
        public async Task GetProviders_ReturnsProvidersWithRequiredFields()
        {
            var response = await this._client.GetAsync("/api/providers");
            response.EnsureSuccessStatusCode();

            var content = await response.Content.ReadFromJsonAsync<JsonElement>();
            var providers = content.GetProperty("providers").EnumerateArray().ToList();

            Assert.True(providers.Count > 0);

            var firstProvider = providers.First();
            Assert.True(firstProvider.TryGetProperty("id", out _));
            Assert.True(firstProvider.TryGetProperty("name", out _));
            Assert.True(firstProvider.TryGetProperty("status", out _));
            Assert.True(firstProvider.TryGetProperty("tier", out _));
            Assert.True(firstProvider.TryGetProperty("models", out _));
            Assert.True(firstProvider.TryGetProperty("usage", out _));
        }

        [Fact]
        public async Task GetProviders_StatusIsHealthyOrUnhealthy()
        {
            var response = await this._client.GetAsync("/api/providers");
            response.EnsureSuccessStatusCode();

            var content = await response.Content.ReadFromJsonAsync<JsonElement>();
            var providers = content.GetProperty("providers").EnumerateArray().ToList();

            Assert.True(providers.Count > 0);

            foreach (var provider in providers)
            {
                var status = provider.GetProperty("status").GetString();
                Assert.True(
string.Equals(status, "healthy", StringComparison.Ordinal) || string.Equals(status, "unhealthy", StringComparison.Ordinal),
                    $"Provider status should be 'healthy' or 'unhealthy', got '{status}'");
            }
        }

        [Fact]
        public async Task GetProviders_UsageHasRequiredFields()
        {
            var response = await this._client.GetAsync("/api/providers");
            response.EnsureSuccessStatusCode();

            var content = await response.Content.ReadFromJsonAsync<JsonElement>();
            var providers = content.GetProperty("providers").EnumerateArray().ToList();

            Assert.True(providers.Count > 0);

            var firstProvider = providers.First();
            var usage = firstProvider.GetProperty("usage");

            Assert.True(usage.TryGetProperty("totalTokens", out _));
            Assert.True(usage.TryGetProperty("requests", out _));
        }

        [Fact]
        public async Task GetProviderStatus_ValidProvider_ReturnsStatus()
        {
            var providersResponse = await this._client.GetAsync("/api/providers");
            providersResponse.EnsureSuccessStatusCode();

            var providersContent = await providersResponse.Content.ReadFromJsonAsync<JsonElement>();
            var providers = providersContent.GetProperty("providers").EnumerateArray().ToList();
            var providerId = providers.First().GetProperty("id").GetString();

            var response = await this._client.GetAsync($"/api/providers/{providerId}/status");

            response.EnsureSuccessStatusCode();
            var content = await response.Content.ReadFromJsonAsync<JsonElement>();

            Assert.True(content.TryGetProperty("status", out var status));
            Assert.True(content.TryGetProperty("lastChecked", out var lastChecked));

            var statusValue = status.GetString();
            Assert.True(string.Equals(statusValue, "healthy", StringComparison.Ordinal) || string.Equals(statusValue, "unhealthy", StringComparison.Ordinal));

            Assert.False(string.IsNullOrEmpty(lastChecked.GetString()));
        }

        [Fact]
        public async Task GetProviderStatus_InvalidProvider_ReturnsNotFound()
        {
            var response = await this._client.GetAsync("/api/providers/nonexistent/status");

            Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        }

        [Fact]
        public async Task UpdateProviderConfig_ValidProvider_UpdatesSuccessfully()
        {
            var providersResponse = await this._client.GetAsync("/api/providers");
            providersResponse.EnsureSuccessStatusCode();

            var providersContent = await providersResponse.Content.ReadFromJsonAsync<JsonElement>();
            var providers = providersContent.GetProperty("providers").EnumerateArray().ToList();
            var providerId = providers.First().GetProperty("id").GetString();

            var updateRequest = new { enabled = true, tier = 1 };
            var response = await this._client.PutAsJsonAsync($"/api/providers/{providerId}/config", updateRequest);

            response.EnsureSuccessStatusCode();
            var content = await response.Content.ReadFromJsonAsync<JsonElement>();

            Assert.True(content.TryGetProperty("success", out var success));
            Assert.True(success.GetBoolean());
        }

        [Fact]
        public async Task UpdateProviderConfig_InvalidProvider_ReturnsNotFound()
        {
            var updateRequest = new { enabled = true, tier = 1 };
            var response = await this._client.PutAsJsonAsync("/api/providers/nonexistent/config", updateRequest);

            Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        }

        [Fact]
        public async Task UpdateProviderConfig_EnabledOnly_UpdatesSuccessfully()
        {
            var providersResponse = await this._client.GetAsync("/api/providers");
            providersResponse.EnsureSuccessStatusCode();

            var providersContent = await providersResponse.Content.ReadFromJsonAsync<JsonElement>();
            var providers = providersContent.GetProperty("providers").EnumerateArray().ToList();
            var providerId = providers.First().GetProperty("id").GetString();

            var updateRequest = new { enabled = false };
            var response = await this._client.PutAsJsonAsync($"/api/providers/{providerId}/config", updateRequest);

            response.EnsureSuccessStatusCode();
            var content = await response.Content.ReadFromJsonAsync<JsonElement>();

            Assert.True(content.TryGetProperty("success", out var success));
            Assert.True(success.GetBoolean());
        }

        [Fact]
        public async Task UpdateProviderConfig_TierOnly_UpdatesSuccessfully()
        {
            var providersResponse = await this._client.GetAsync("/api/providers");
            providersResponse.EnsureSuccessStatusCode();

            var providersContent = await providersResponse.Content.ReadFromJsonAsync<JsonElement>();
            var providers = providersContent.GetProperty("providers").EnumerateArray().ToList();
            var providerId = providers.First().GetProperty("id").GetString();

            var updateRequest = new { tier = 2 };
            var response = await this._client.PutAsJsonAsync($"/api/providers/{providerId}/config", updateRequest);

            response.EnsureSuccessStatusCode();
            var content = await response.Content.ReadFromJsonAsync<JsonElement>();

            Assert.True(content.TryGetProperty("success", out var success));
            Assert.True(success.GetBoolean());
        }

        [Fact]
        public async Task GetProviders_MatchesExpectedResponseFormat()
        {
            var response = await this._client.GetAsync("/api/providers");
            response.EnsureSuccessStatusCode();

            var content = await response.Content.ReadAsStringAsync();
            var json = JsonDocument.Parse(content);
            var root = json.RootElement;

            Assert.True(root.TryGetProperty("providers", out var providers));

            var providerList = providers.EnumerateArray().ToList();
            if (providerList.Count > 0)
            {
                var sampleProvider = providerList.First();

                Assert.True(sampleProvider.GetProperty("id").ValueKind == JsonValueKind.String);
                Assert.True(sampleProvider.GetProperty("name").ValueKind == JsonValueKind.String);
                Assert.True(sampleProvider.GetProperty("status").ValueKind == JsonValueKind.String);
                Assert.True(sampleProvider.GetProperty("tier").ValueKind == JsonValueKind.Number);
                Assert.True(sampleProvider.GetProperty("models").ValueKind == JsonValueKind.Array);
                Assert.True(sampleProvider.GetProperty("usage").ValueKind == JsonValueKind.Object);

                var usage = sampleProvider.GetProperty("usage");
                Assert.True(usage.GetProperty("totalTokens").ValueKind == JsonValueKind.Number);
                Assert.True(usage.GetProperty("requests").ValueKind == JsonValueKind.Number);
            }
        }

        [Fact]
        public async Task ProvidersEndpoints_AreAccessibleWithoutAuth()
        {
            var getProvidersResponse = await this._client.GetAsync("/api/providers");
            Assert.Equal(HttpStatusCode.OK, getProvidersResponse.StatusCode);

            var providersContent = await getProvidersResponse.Content.ReadFromJsonAsync<JsonElement>();
            var providers = providersContent.GetProperty("providers").EnumerateArray().ToList();
            var providerId = providers.First().GetProperty("id").GetString();

            var getStatusResponse = await this._client.GetAsync($"/api/providers/{providerId}/status");
            Assert.Equal(HttpStatusCode.OK, getStatusResponse.StatusCode);

            var updateRequest = new { enabled = true };
            var putConfigResponse = await this._client.PutAsJsonAsync($"/api/providers/{providerId}/config", updateRequest);
            Assert.Equal(HttpStatusCode.OK, putConfigResponse.StatusCode);
        }
    }
}
