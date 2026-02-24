// <copyright file="ApiErrorHandlingTests.cs" company="PlaceholderCompany">
// Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>

using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Xunit.Abstractions;

namespace Synaxis.InferenceGateway.IntegrationTests;

[Collection("Integration")]
public class ApiErrorHandlingTests
{
    private readonly SynaxisWebApplicationFactory _factory;
    private readonly HttpClient _client;

    public ApiErrorHandlingTests(SynaxisWebApplicationFactory factory, ITestOutputHelper output)
    {
        this._factory = factory;
        this._factory.OutputHelper = output;
        this._client = factory.CreateClient();
    }

    [Fact]
    public async Task Get_NonExistentEndpoint_Returns404()
    {
        // Act
        var response = await this._client.GetAsync("/openai/v1/non-existent");

        // Assert
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task Post_InvalidEndpoint_Returns404()
    {
        // Act
        var response = await this._client.PostAsJsonAsync("/openai/v1/invalid", new { });

        // Assert
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task Get_Models_ReturnsValidJson()
    {
        // Act
        var response = await this._client.GetAsync("/openai/v1/models");

        // Assert
        response.EnsureSuccessStatusCode();
        var content = await response.Content.ReadAsStringAsync();
        Assert.False(string.IsNullOrEmpty(content));

        // Verify it's valid JSON
        var json = JsonDocument.Parse(content);
        Assert.True(json.RootElement.TryGetProperty("data", out _));
    }

    [Fact]
    public async Task Get_ModelById_InvalidModel_Returns404()
    {
        // Act
        var response = await this._client.GetAsync("/openai/v1/models/non-existent-model-12345");

        // Assert
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task Post_ChatCompletions_MalformedJson_Returns400()
    {
        // Arrange
        var content = new StringContent("{invalid json", System.Text.Encoding.UTF8, "application/json");

        // Act
        var response = await this._client.PostAsync("/openai/v1/chat/completions", content);

        // Assert - Malformed JSON must return 400 Bad Request (client error)
        var responseBody = await response.Content.ReadAsStringAsync();
        this._factory.OutputHelper?.WriteLine($"Response: {responseBody}");
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Post_ChatCompletions_EmptyBody_Returns400()
    {
        // Arrange
        var content = new StringContent(string.Empty, System.Text.Encoding.UTF8, "application/json");

        // Act
        var response = await this._client.PostAsync("/openai/v1/chat/completions", content);

        // Assert
        Assert.True(
            response.StatusCode == HttpStatusCode.BadRequest ||
            response.StatusCode == HttpStatusCode.UnsupportedMediaType,
            $"Expected 400 or 415 but got {(int)response.StatusCode}");
    }

    [Fact]
    public async Task HealthChecks_Liveness_Returns200()
    {
        // Act
        var response = await this._client.GetAsync("/health/liveness");

        // Assert
        response.EnsureSuccessStatusCode();
    }

    [Fact]
    public async Task Response_ContentType_IsApplicationJson()
    {
        // Act
        var response = await this._client.GetAsync("/openai/v1/models");

        // Assert
        response.EnsureSuccessStatusCode();
        Assert.Contains("application/json", response.Content.Headers.ContentType?.MediaType ?? string.Empty, StringComparison.Ordinal);
    }
}
