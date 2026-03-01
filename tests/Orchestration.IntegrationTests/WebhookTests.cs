// <copyright file="WebhookTests.cs" company="Synaxis">
// Copyright (c) Synaxis. All rights reserved.
// </copyright>

namespace Orchestration.IntegrationTests;

using FluentAssertions;
using Orchestration.IntegrationTests.Fixtures;
using System.Net;
using System.Text;
using System.Text.Json;
using Xunit;

/// <summary>
/// Integration tests for the Webhooks API endpoints.
/// </summary>
[Collection("Orchestration")]
public class WebhookTests
{
    private readonly OrchestrationTestFixture _fixture;
    private readonly HttpClient _client;

    /// <summary>
    /// Initializes a new instance of the <see cref="WebhookTests"/> class.
    /// </summary>
    /// <param name="fixture">The shared test fixture.</param>
    public WebhookTests(OrchestrationTestFixture fixture)
    {
        _fixture = fixture ?? throw new ArgumentNullException(nameof(fixture));
        _client = _fixture.Client;
    }

    /// <summary>
    /// Verifies that GET /api/v1/webhooks returns OK status.
    /// </summary>
    [Fact]
    public async Task GetWebhooks_ReturnsOk()
    {
        // Act
        var response = await _client.GetAsync("/api/v1/webhooks");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    /// <summary>
    /// Verifies that GET /api/v1/webhooks/{id} returns OK or NotFound
    /// depending on whether the webhook exists.
    /// </summary>
    [Fact]
    public async Task GetWebhook_WithValidId_ReturnsOkOrNotFound()
    {
        // Arrange
        var webhookId = Guid.NewGuid();

        // Act
        var response = await _client.GetAsync($"/api/v1/webhooks/{webhookId}");

        // Assert
        response.StatusCode.Should().BeOneOf(HttpStatusCode.OK, HttpStatusCode.NotFound);
    }

    /// <summary>
    /// Verifies that POST /api/v1/webhooks with valid request returns Created status.
    /// </summary>
    [Fact]
    public async Task CreateWebhook_WithValidRequest_ReturnsCreated()
    {
        // Arrange
        var request = new
        {
            url = "https://example.com/webhook",
            eventType = "JobCompleted",
            secret = "my-secret"
        };
        var content = new StringContent(
            JsonSerializer.Serialize(request),
            Encoding.UTF8,
            "application/json");

        // Act
        var response = await _client.PostAsync("/api/v1/webhooks", content);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Created);
    }

    /// <summary>
    /// Verifies that PUT /api/v1/webhooks/{id} returns NoContent or NotFound
    /// depending on whether the webhook exists.
    /// </summary>
    [Fact]
    public async Task UpdateWebhook_WithValidId_ReturnsNoContentOrNotFound()
    {
        // Arrange
        var webhookId = Guid.NewGuid();
        var request = new
        {
            url = "https://example.com/webhook-updated",
            eventType = "JobCompleted",
            isActive = true
        };
        var content = new StringContent(
            JsonSerializer.Serialize(request),
            Encoding.UTF8,
            "application/json");

        // Act
        var response = await _client.PutAsync($"/api/v1/webhooks/{webhookId}", content);

        // Assert
        response.StatusCode.Should().BeOneOf(HttpStatusCode.NoContent, HttpStatusCode.NotFound);
    }

    /// <summary>
    /// Verifies that DELETE /api/v1/webhooks/{id} returns NoContent or NotFound
    /// depending on whether the webhook exists.
    /// </summary>
    [Fact]
    public async Task DeleteWebhook_WithValidId_ReturnsNoContentOrNotFound()
    {
        // Arrange
        var webhookId = Guid.NewGuid();

        // Act
        var response = await _client.DeleteAsync($"/api/v1/webhooks/{webhookId}");

        // Assert
        response.StatusCode.Should().BeOneOf(HttpStatusCode.NoContent, HttpStatusCode.NotFound);
    }

    /// <summary>
    /// Verifies that POST /api/v1/webhooks/{id}/test returns OK or NotFound
    /// depending on whether the webhook exists.
    /// </summary>
    [Fact]
    public async Task TestWebhook_WithValidId_ReturnsOkOrNotFound()
    {
        // Arrange
        var webhookId = Guid.NewGuid();

        // Act
        var response = await _client.PostAsync($"/api/v1/webhooks/{webhookId}/test", null);

        // Assert
        response.StatusCode.Should().BeOneOf(HttpStatusCode.OK, HttpStatusCode.NotFound);
    }
}
