// <copyright file="OutboxTests.cs" company="Synaxis">
// Copyright (c) Synaxis. All rights reserved.
// </copyright>

namespace Orchestration.IntegrationTests;

using FluentAssertions;
using Orchestration.IntegrationTests.Fixtures;
using System.Net;
using Xunit;

/// <summary>
/// Integration tests for the Outbox API endpoints.
/// </summary>
[Collection("Orchestration")]
public class OutboxTests
{
    private readonly OrchestrationTestFixture _fixture;
    private readonly HttpClient _client;

    /// <summary>
    /// Initializes a new instance of the <see cref="OutboxTests"/> class.
    /// </summary>
    /// <param name="fixture">The shared test fixture.</param>
    public OutboxTests(OrchestrationTestFixture fixture)
    {
        _fixture = fixture ?? throw new ArgumentNullException(nameof(fixture));
        _client = _fixture.Client;
    }

    /// <summary>
    /// Verifies that GET /api/v1/outbox returns OK status.
    /// </summary>
    [Fact]
    public async Task GetMessages_ReturnsOk()
    {
        // Act
        var response = await _client.GetAsync("/api/v1/outbox");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    /// <summary>
    /// Verifies that GET /api/v1/outbox/pending returns OK status.
    /// </summary>
    [Fact]
    public async Task GetPendingMessages_ReturnsOk()
    {
        // Act
        var response = await _client.GetAsync("/api/v1/outbox/pending");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    /// <summary>
    /// Verifies that GET /api/v1/outbox/failed returns OK status.
    /// </summary>
    [Fact]
    public async Task GetFailedMessages_ReturnsOk()
    {
        // Act
        var response = await _client.GetAsync("/api/v1/outbox/failed");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    /// <summary>
    /// Verifies that POST /api/v1/outbox/{id}/retry returns NoContent or NotFound
    /// depending on whether the message exists.
    /// </summary>
    [Fact]
    public async Task RetryMessage_WithValidId_ReturnsNoContentOrNotFound()
    {
        // Arrange
        var messageId = Guid.NewGuid();

        // Act
        var response = await _client.PostAsync($"/api/v1/outbox/{messageId}/retry", null);

        // Assert
        response.StatusCode.Should().BeOneOf(HttpStatusCode.NoContent, HttpStatusCode.NotFound);
    }
}
