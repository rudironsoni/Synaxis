// <copyright file="ResponsesEndpointTests.cs" company="PlaceholderCompany">
// Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>

using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Synaxis.InferenceGateway.Application;
using Synaxis.InferenceGateway.IntegrationTests.SmokeTests.Infrastructure;
using Xunit;

namespace Synaxis.InferenceGateway.IntegrationTests.Endpoints;

[Collection("Integration")]
public class ResponsesEndpointTests : IClassFixture<SynaxisWebApplicationFactory>
{
    private readonly SynaxisWebApplicationFactory _factory;
    private readonly HttpClient _client;

    public ResponsesEndpointTests(SynaxisWebApplicationFactory factory)
    {
        this._factory = factory;
        this._client = factory.WithWebHostBuilder(builder =>
        {
            builder.ConfigureAppConfiguration((context, config) =>
            {
                config.AddInMemoryCollection(new Dictionary<string, string?>
(StringComparer.Ordinal)
                {
                    ["Synaxis:InferenceGateway:CanonicalModels:0:Id"] = "test-provider/model",
                    ["Synaxis:InferenceGateway:CanonicalModels:0:Provider"] = "test-provider",
                    ["Synaxis:InferenceGateway:CanonicalModels:0:ModelPath"] = "model",
                    ["Synaxis:InferenceGateway:CanonicalModels:0:Streaming"] = "true",
                    ["Synaxis:InferenceGateway:Aliases:test-alias:Candidates:0"] = "test-provider/model",
                    ["Synaxis:InferenceGateway:Aliases:default:Candidates:0"] = "test-provider/model",

                    ["Synaxis:InferenceGateway:CanonicalModels:1:Id"] = "test-provider/no-stream",
                    ["Synaxis:InferenceGateway:CanonicalModels:1:Provider"] = "test-provider",
                    ["Synaxis:InferenceGateway:CanonicalModels:1:ModelPath"] = "no-stream",
                    ["Synaxis:InferenceGateway:CanonicalModels:1:Streaming"] = "false",

                    ["Synaxis:InferenceGateway:Providers:test-provider:Enabled"] = "true",
                    ["Synaxis:InferenceGateway:Providers:test-provider:Type"] = "mock",
                    ["Synaxis:InferenceGateway:Providers:test-provider:Tier"] = "1",
                    ["Synaxis:InferenceGateway:Providers:test-provider:Models:0"] = "test-provider/model",
                    ["Synaxis:InferenceGateway:Providers:test-provider:Models:1"] = "test-provider/no-stream",
                });
            });

            builder.ConfigureTestServices(services =>
            {
                services.AddSingleton<IProviderRegistry, Synaxis.InferenceGateway.IntegrationTests.MockProviderRegistry>();
                services.AddKeyedSingleton<IChatClient>("test-provider", new Synaxis.InferenceGateway.IntegrationTests.MockChatClient());
            });
        }).CreateClient();
    }

    [Fact]
    public async Task PostResponses_WithoutAuth_ReturnsResponse()
    {
        var request = new
        {
            model = "test-alias",
            messages = new[] { new { role = "user", content = "Hello" } },
        };

        var response = await this._client.PostAsJsonAsync("/openai/v1/responses", request);

        response.EnsureSuccessStatusCode();
        var content = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal("response", content.GetProperty("object").GetString());
    }

    [Fact]
    public async Task PostResponses_WithAuth_ReturnsResponse()
    {
        var token = await this.GetAuthTokenAsync();
        var authenticatedClient = this.CreateAuthenticatedClient(token);

        var request = new
        {
            model = "test-alias",
            messages = new[] { new { role = "user", content = "Hello" } },
        };

        var response = await authenticatedClient.PostAsJsonAsync("/openai/v1/responses", request);

        response.EnsureSuccessStatusCode();
        var content = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.True(content.TryGetProperty("id", out _));
        Assert.True(content.TryGetProperty("object", out _));
        Assert.Equal("response", content.GetProperty("object").GetString());
    }

    [Fact]
    public async Task PostResponses_WithAuth_ContainsOutput()
    {
        var token = await this.GetAuthTokenAsync();
        var authenticatedClient = this.CreateAuthenticatedClient(token);

        var request = new
        {
            model = "test-alias",
            messages = new[] { new { role = "user", content = "Hello" } },
        };

        var response = await authenticatedClient.PostAsJsonAsync("/openai/v1/responses", request);
        response.EnsureSuccessStatusCode();

        var content = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.True(content.TryGetProperty("output", out var output));
        Assert.Equal(JsonValueKind.Array, output.ValueKind);
        Assert.True(output.GetArrayLength() > 0);
    }

    [Fact]
    public async Task PostResponses_WithAuth_OutputHasMessage()
    {
        var token = await this.GetAuthTokenAsync();
        var authenticatedClient = this.CreateAuthenticatedClient(token);

        var request = new
        {
            model = "test-alias",
            messages = new[] { new { role = "user", content = "Hello" } },
        };

        var response = await authenticatedClient.PostAsJsonAsync("/openai/v1/responses", request);
        response.EnsureSuccessStatusCode();

        var content = await response.Content.ReadFromJsonAsync<JsonElement>();
        var output = content.GetProperty("output").EnumerateArray().First();
        Assert.True(output.TryGetProperty("type", out _));
        Assert.Equal("message", output.GetProperty("type").GetString());
        Assert.True(output.TryGetProperty("role", out _));
        Assert.Equal("assistant", output.GetProperty("role").GetString());
    }

    [Fact]
    public async Task PostResponses_WithAuth_ContentHasText()
    {
        var token = await this.GetAuthTokenAsync();
        var authenticatedClient = this.CreateAuthenticatedClient(token);

        var request = new
        {
            model = "test-alias",
            messages = new[] { new { role = "user", content = "Hello" } },
        };

        var response = await authenticatedClient.PostAsJsonAsync("/openai/v1/responses", request);
        response.EnsureSuccessStatusCode();

        var content = await response.Content.ReadFromJsonAsync<JsonElement>();
        var output = content.GetProperty("output").EnumerateArray().First();
        var contentArray = output.GetProperty("content");
        Assert.Equal(JsonValueKind.Array, contentArray.ValueKind);
        Assert.True(contentArray.GetArrayLength() > 0);

        var firstContent = contentArray.EnumerateArray().First();
        Assert.True(firstContent.TryGetProperty("type", out _));
        Assert.Equal("output_text", firstContent.GetProperty("type").GetString());
        Assert.True(firstContent.TryGetProperty("text", out _));
    }

    [Fact]
    public async Task PostResponses_WithStreaming_ReturnsStream()
    {
        var token = await this.GetAuthTokenAsync();
        var authenticatedClient = this.CreateAuthenticatedClient(token);

        var request = new
        {
            model = "test-alias",
            messages = new[] { new { role = "user", content = "Hello" } },
            stream = true,
        };

        var response = await authenticatedClient.PostAsJsonAsync("/openai/v1/responses", request);

        response.EnsureSuccessStatusCode();
        Assert.Equal("text/event-stream", response.Content.Headers.ContentType?.MediaType);
    }

    [Fact]
    public async Task PostResponses_EmptyModel_UsesDefault()
    {
        var token = await this.GetAuthTokenAsync();
        var authenticatedClient = this.CreateAuthenticatedClient(token);

        var request = new
        {
            model = string.Empty,
            messages = new[] { new { role = "user", content = "Hello" } },
        };

        var response = await authenticatedClient.PostAsJsonAsync("/openai/v1/responses", request);

        response.EnsureSuccessStatusCode();
        var content = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal("response", content.GetProperty("object").GetString());
    }

    [Fact]
    public async Task PostResponses_MissingMessages_ReturnsResponse()
    {
        var token = await this.GetAuthTokenAsync();
        var authenticatedClient = this.CreateAuthenticatedClient(token);

        var request = new
        {
            model = "test-alias",
        };

        var response = await authenticatedClient.PostAsJsonAsync("/openai/v1/responses", request);

        response.EnsureSuccessStatusCode();
        var content = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal("response", content.GetProperty("object").GetString());
    }

    private async Task<string> GetAuthTokenAsync()
    {
        var loginRequest = new { Email = "test@example.com" };
        var response = await _client.PostAsJsonAsync("/auth/dev-login", loginRequest).ConfigureAwait(false);
        response.EnsureSuccessStatusCode();

        var content = await response.Content.ReadFromJsonAsync<JsonElement>().ConfigureAwait(false);
        return content.GetProperty("token").GetString()!;
    }

    private HttpClient CreateAuthenticatedClient(string token)
    {
        var client = this._factory.WithWebHostBuilder(builder =>
        {
            builder.ConfigureAppConfiguration((context, config) =>
            {
                config.AddInMemoryCollection(new Dictionary<string, string?>
(StringComparer.Ordinal)
                {
                    ["Synaxis:InferenceGateway:CanonicalModels:0:Id"] = "test-provider/model",
                    ["Synaxis:InferenceGateway:CanonicalModels:0:Provider"] = "test-provider",
                    ["Synaxis:InferenceGateway:CanonicalModels:0:ModelPath"] = "model",
                    ["Synaxis:InferenceGateway:CanonicalModels:0:Streaming"] = "true",
                    ["Synaxis:InferenceGateway:Aliases:test-alias:Candidates:0"] = "test-provider/model",
                    ["Synaxis:InferenceGateway:Aliases:default:Candidates:0"] = "test-provider/model",

                    ["Synaxis:InferenceGateway:CanonicalModels:1:Id"] = "test-provider/no-stream",
                    ["Synaxis:InferenceGateway:CanonicalModels:1:Provider"] = "test-provider",
                    ["Synaxis:InferenceGateway:CanonicalModels:1:ModelPath"] = "no-stream",
                    ["Synaxis:InferenceGateway:CanonicalModels:1:Streaming"] = "false",

                    ["Synaxis:InferenceGateway:Providers:test-provider:Enabled"] = "true",
                    ["Synaxis:InferenceGateway:Providers:test-provider:Type"] = "mock",
                    ["Synaxis:InferenceGateway:Providers:test-provider:Tier"] = "1",
                    ["Synaxis:InferenceGateway:Providers:test-provider:Models:0"] = "test-provider/model",
                    ["Synaxis:InferenceGateway:Providers:test-provider:Models:1"] = "test-provider/no-stream",
                });
            });

            builder.ConfigureTestServices(services =>
            {
                services.AddSingleton<IProviderRegistry, Synaxis.InferenceGateway.IntegrationTests.MockProviderRegistry>();
                services.AddKeyedSingleton<IChatClient>("test-provider", new Synaxis.InferenceGateway.IntegrationTests.MockChatClient());
            });
        }).CreateClient();
        client.DefaultRequestHeaders.Authorization =
            new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);
        return client;
    }
}
