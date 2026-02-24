// <copyright file="ModelsEndpointTests.cs" company="PlaceholderCompany">
// Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>

using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Synaxis.InferenceGateway.IntegrationTests.SmokeTests.Infrastructure;
using Xunit;

namespace Synaxis.InferenceGateway.IntegrationTests.Endpoints;

[Collection("Integration")]
public class ModelsEndpointTests(SynaxisWebApplicationFactory factory)
{
    private readonly SynaxisWebApplicationFactory _factory = factory;
    private readonly HttpClient _client = factory.CreateClient();

    [Fact]
    public async Task GetModels_ReturnsList()
    {
        var response = await this._client.GetAsync("/openai/v1/models");

        response.EnsureSuccessStatusCode();
        var content = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal(JsonValueKind.Object, content.ValueKind);
        Assert.True(content.TryGetProperty("object", out var obj));
        Assert.Equal("list", obj.GetString());
        Assert.True(content.TryGetProperty("data", out var data));
        Assert.Equal(JsonValueKind.Array, data.ValueKind);
    }

    [Fact]
    public async Task GetModels_ContainsCanonicalModels()
    {
        var response = await this._client.GetAsync("/openai/v1/models");
        response.EnsureSuccessStatusCode();

        var content = await response.Content.ReadFromJsonAsync<JsonElement>();
        var models = content.GetProperty("data");

        Assert.True(models.GetArrayLength() > 0);
    }

    [Fact]
    public async Task GetModels_ModelsHaveRequiredFields()
    {
        var response = await this._client.GetAsync("/openai/v1/models");
        response.EnsureSuccessStatusCode();

        var content = await response.Content.ReadFromJsonAsync<JsonElement>();
        var models = content.GetProperty("data").EnumerateArray();

        var firstModel = models.First();
        Assert.True(firstModel.TryGetProperty("id", out _));
        Assert.True(firstModel.TryGetProperty("object", out _));
        Assert.True(firstModel.TryGetProperty("created", out _));
        Assert.True(firstModel.TryGetProperty("owned_by", out _));
        Assert.True(firstModel.TryGetProperty("provider", out _));
        Assert.True(firstModel.TryGetProperty("model_path", out _));
    }

    [Fact]
    public async Task GetModels_ModelsHaveCapabilities()
    {
        var response = await this._client.GetAsync("/openai/v1/models");
        response.EnsureSuccessStatusCode();

        var content = await response.Content.ReadFromJsonAsync<JsonElement>();
        var models = content.GetProperty("data").EnumerateArray();

        var firstModel = models.First();
        Assert.True(firstModel.TryGetProperty("capabilities", out var capabilities));
        Assert.Equal(JsonValueKind.Object, capabilities.ValueKind);
        Assert.True(capabilities.TryGetProperty("streaming", out _));
        Assert.True(capabilities.TryGetProperty("tools", out _));
        Assert.True(capabilities.TryGetProperty("vision", out _));
        Assert.True(capabilities.TryGetProperty("structured_output", out _));
        Assert.True(capabilities.TryGetProperty("log_probs", out _));
    }

    [Fact]
    public async Task GetModels_ContainsProviders()
    {
        var response = await this._client.GetAsync("/openai/v1/models");
        response.EnsureSuccessStatusCode();

        var content = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.True(content.TryGetProperty("providers", out var providers));
        Assert.Equal(JsonValueKind.Array, providers.ValueKind);
    }

    [Fact]
    public async Task GetModels_ProvidersHaveRequiredFields()
    {
        var response = await this._client.GetAsync("/openai/v1/models");
        response.EnsureSuccessStatusCode();

        var content = await response.Content.ReadFromJsonAsync<JsonElement>();
        var providers = content.GetProperty("providers").EnumerateArray();

        if (providers.Any())
        {
            var firstProvider = providers.First();
            Assert.True(firstProvider.TryGetProperty("id", out _));
            Assert.True(firstProvider.TryGetProperty("type", out _));
            Assert.True(firstProvider.TryGetProperty("enabled", out _));
            Assert.True(firstProvider.TryGetProperty("tier", out _));
        }
    }

    [Fact]
    public async Task GetModelById_ReturnsModel()
    {
        var listResponse = await this._client.GetAsync("/openai/v1/models");
        listResponse.EnsureSuccessStatusCode();
        var listContent = await listResponse.Content.ReadFromJsonAsync<JsonElement>();
        var firstModelId = listContent.GetProperty("data").EnumerateArray().First().GetProperty("id").GetString();

        // Do not pre-encode the model id; HttpClient will handle URL encoding and the endpoint
        // uses a catch-all route pattern that expects raw segments.
        var response = await this._client.GetAsync($"/openai/v1/models/{firstModelId}");

        response.EnsureSuccessStatusCode();
        var content = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.True(content.TryGetProperty("id", out _));
        Assert.Equal(firstModelId, content.GetProperty("id").GetString());
    }

    [Fact]
    public async Task GetModelById_ReturnsCapabilities()
    {
        var listResponse = await this._client.GetAsync("/openai/v1/models");
        listResponse.EnsureSuccessStatusCode();
        var listContent = await listResponse.Content.ReadFromJsonAsync<JsonElement>();
        var firstModelId = listContent.GetProperty("data").EnumerateArray().First().GetProperty("id").GetString();
        var response = await this._client.GetAsync($"/openai/v1/models/{firstModelId}");
        response.EnsureSuccessStatusCode();

        var content = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.True(content.TryGetProperty("capabilities", out var capabilities));
        Assert.Equal(JsonValueKind.Object, capabilities.ValueKind);
    }

    [Fact]
    public async Task GetModelById_InvalidModel_ReturnsNotFound()
    {
        var response = await this._client.GetAsync("/openai/v1/models/nonexistent-model-12345");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task GetModelById_ReturnsModelPath()
    {
        var listResponse = await this._client.GetAsync("/openai/v1/models");
        listResponse.EnsureSuccessStatusCode();
        var listContent = await listResponse.Content.ReadFromJsonAsync<JsonElement>();
        var firstModel = listContent.GetProperty("data").EnumerateArray().First();
        var firstModelId = firstModel.GetProperty("id").GetString();
        var response = await this._client.GetAsync($"/openai/v1/models/{firstModelId}");
        response.EnsureSuccessStatusCode();

        var content = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.True(content.TryGetProperty("model_path", out var modelPath));
        Assert.False(string.IsNullOrEmpty(modelPath.GetString()));
    }

    [Fact]
    public async Task GetModelById_ReturnsProvider()
    {
        var listResponse = await this._client.GetAsync("/openai/v1/models");
        listResponse.EnsureSuccessStatusCode();
        var listContent = await listResponse.Content.ReadFromJsonAsync<JsonElement>();
        var firstModel = listContent.GetProperty("data").EnumerateArray().First();
        var firstModelId = firstModel.GetProperty("id").GetString();
        var response = await this._client.GetAsync($"/openai/v1/models/{firstModelId}");
        response.EnsureSuccessStatusCode();

        var content = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.True(content.TryGetProperty("provider", out var provider));
        Assert.False(string.IsNullOrEmpty(provider.GetString()));
    }

    [Fact]
    public async Task GetModels_ContainsAliases()
    {
        var response = await this._client.GetAsync("/openai/v1/models");
        response.EnsureSuccessStatusCode();

        var content = await response.Content.ReadFromJsonAsync<JsonElement>();
        var models = content.GetProperty("data").EnumerateArray();

        var aliasModels = models.Where(m => string.Equals(m.GetProperty("owned_by").GetString(), "synaxis", StringComparison.Ordinal)).ToList();
        Assert.True(aliasModels.Count > 0, "Should contain at least one alias model (e.g., 'default')");
    }

    [Fact]
    public async Task GetModels_AliasesHaveSynaxisOwnedBy()
    {
        var response = await this._client.GetAsync("/openai/v1/models");
        response.EnsureSuccessStatusCode();

        var content = await response.Content.ReadFromJsonAsync<JsonElement>();
        var models = content.GetProperty("data").EnumerateArray();

        var aliasModel = models.First(m => string.Equals(m.GetProperty("owned_by").GetString(), "synaxis", StringComparison.Ordinal));
        Assert.Equal("synaxis", aliasModel.GetProperty("provider").GetString());
    }

    [Fact]
    public async Task GetModels_CapabilitiesAreBoolean()
    {
        var response = await this._client.GetAsync("/openai/v1/models");
        response.EnsureSuccessStatusCode();

        var content = await response.Content.ReadFromJsonAsync<JsonElement>();
        var models = content.GetProperty("data").EnumerateArray();

        var canonicalModel = models.First(m => !string.Equals(m.GetProperty("owned_by").GetString(), "synaxis", StringComparison.Ordinal));
        var capabilities = canonicalModel.GetProperty("capabilities");

        // streaming may be true or false depending on provider; accept either boolean kind
        var streamingKind = capabilities.GetProperty("streaming").ValueKind;
        Assert.True(
            streamingKind == JsonValueKind.True || streamingKind == JsonValueKind.False,
            "Expected 'streaming' capability to be a boolean (true or false)");
    }
}
