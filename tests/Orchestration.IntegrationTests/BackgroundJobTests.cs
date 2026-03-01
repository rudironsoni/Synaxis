// <copyright file="BackgroundJobTests.cs" company="Synaxis">
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
/// Integration tests for the Background Jobs API endpoints.
/// </summary>
[Collection("Orchestration")]
public class BackgroundJobTests
{
    private readonly OrchestrationTestFixture _fixture;
    private readonly HttpClient _client;

    /// <summary>
    /// Initializes a new instance of the <see cref="BackgroundJobTests"/> class.
    /// </summary>
    /// <param name="fixture">The shared test fixture.</param>
    public BackgroundJobTests(OrchestrationTestFixture fixture)
    {
        _fixture = fixture ?? throw new ArgumentNullException(nameof(fixture));
        _client = _fixture.Client;
    }

    /// <summary>
    /// Verifies that GET /api/v1/backgroundjobs returns OK status.
    /// </summary>
    [Fact]
    public async Task GetJobs_ReturnsOk()
    {
        // Act
        var response = await _client.GetAsync("/api/v1/backgroundjobs");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    /// <summary>
    /// Verifies that GET /api/v1/backgroundjobs/{id} returns OK or NotFound
    /// depending on whether the job exists.
    /// </summary>
    [Fact]
    public async Task GetJob_WithValidId_ReturnsOkOrNotFound()
    {
        // Arrange
        var jobId = Guid.NewGuid();

        // Act
        var response = await _client.GetAsync($"/api/v1/backgroundjobs/{jobId}");

        // Assert
        response.StatusCode.Should().BeOneOf(HttpStatusCode.OK, HttpStatusCode.NotFound);
    }

    /// <summary>
    /// Verifies that GET /api/v1/backgroundjobs/{id}/status returns OK or NotFound
    /// depending on whether the job exists.
    /// </summary>
    [Fact]
    public async Task GetJobStatus_WithValidId_ReturnsOkOrNotFound()
    {
        // Arrange
        var jobId = Guid.NewGuid();

        // Act
        var response = await _client.GetAsync($"/api/v1/backgroundjobs/{jobId}/status");

        // Assert
        response.StatusCode.Should().BeOneOf(HttpStatusCode.OK, HttpStatusCode.NotFound);
    }

    /// <summary>
    /// Verifies that POST /api/v1/backgroundjobs with valid request returns Created status.
    /// </summary>
    [Fact]
    public async Task CreateJob_WithValidRequest_ReturnsCreated()
    {
        // Arrange
        var request = new
        {
            jobType = "TestJob",
            payload = "{ \"test\": \"data\" }"
        };
        var content = new StringContent(
            JsonSerializer.Serialize(request),
            Encoding.UTF8,
            "application/json");

        // Act
        var response = await _client.PostAsync("/api/v1/backgroundjobs", content);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Created);
    }

    /// <summary>
    /// Verifies that POST /api/v1/backgroundjobs/{id}/cancel returns NoContent or NotFound
    /// depending on whether the job exists.
    /// </summary>
    [Fact]
    public async Task CancelJob_WithValidId_ReturnsNoContentOrNotFound()
    {
        // Arrange
        var jobId = Guid.NewGuid();

        // Act
        var response = await _client.PostAsync($"/api/v1/backgroundjobs/{jobId}/cancel", null);

        // Assert
        response.StatusCode.Should().BeOneOf(HttpStatusCode.NoContent, HttpStatusCode.NotFound);
    }
}
