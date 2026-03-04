// <copyright file="ChatControllerTests.cs" company="Synaxis">
// Copyright (c) Synaxis. All rights reserved.
// </copyright>

namespace Synaxis.Inference.IntegrationTests.Controllers;

using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc.Testing;
using Synaxis.Inference.Api.Controllers;
using Synaxis.Inference.Api.Models;
using Synaxis.Inference.IntegrationTests.Fixtures;
using Xunit;

/// <summary>
/// Integration tests for the ChatController.
/// </summary>
[Trait("Category", "Integration")]
[Collection("IntegrationTests")]
public class ChatControllerTests : IDisposable
{
    private readonly WebApplicationFactory<Api.Program> factory;
    private readonly HttpClient client;

    /// <summary>
    /// Initializes a new instance of the <see cref="ChatControllerTests"/> class.
    /// </summary>
    /// <param name="webApplicationFactory">The web application factory.</param>
    public ChatControllerTests(CustomWebApplicationFactory webApplicationFactory)
    {
        factory = webApplicationFactory;
        client = factory.CreateClient();
    }

    /// <summary>
    /// Disposes the test resources.
    /// </summary>
    public void Dispose()
    {
        client.Dispose();
    }

    #region POST /api/chat/completions

    /// <summary>
    /// Tests that a valid chat completion request returns 200 OK.
    /// </summary>
    [Fact]
    public async Task CreateChatCompletion_WithValidRequest_Returns200Ok()
    {
        // Arrange
        var request = new ChatCompletionRequest
        {
            Model = "gpt-4o",
            Messages =
            [
                new ChatMessage { Role = "system", Content = "You are a helpful assistant." },
                new ChatMessage { Role = "user", Content = "Hello, how are you?" }
            ],
            Temperature = 0.7,
            MaxTokens = 150,
            Stream = false
        };

        // Act
        var response = await client.PostAsJsonAsync("/api/chat/completions", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var responseContent = await response.Content.ReadFromJsonAsync<ChatCompletionResponse>();
        responseContent.Should().NotBeNull();
        responseContent!.Id.Should().StartWith("chatcmpl-");
        responseContent.Model.Should().Be("gpt-4o");
        responseContent.Choices.Should().NotBeNullOrEmpty();
        responseContent.Choices[0].Message.Should().NotBeNull();
        responseContent.Choices[0].FinishReason.Should().Be("stop");
        responseContent.Usage.Should().NotBeNull();
    }

    /// <summary>
    /// Tests that an invalid chat completion request returns 400 Bad Request.
    /// </summary>
    [Fact]
    public async Task CreateChatCompletion_WithInvalidRequest_Returns400BadRequest()
    {
        // Arrange - missing required fields (Model is empty)
        var request = new ChatCompletionRequest
        {
            Model = string.Empty,
            Messages = [],
            Stream = false
        };

        // Act
        var response = await client.PostAsJsonAsync("/api/chat/completions", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    /// <summary>
    /// Tests that a chat completion request with stream=true returns 400 Bad Request (redirecting to stream endpoint).
    /// </summary>
    [Fact]
    public async Task CreateChatCompletion_WithStreamEnabled_Returns400BadRequest()
    {
        // Arrange
        var request = new ChatCompletionRequest
        {
            Model = "gpt-4o",
            Messages =
            [
                new ChatMessage { Role = "user", Content = "Hello" }
            ],
            Stream = true
        };

        // Act
        var response = await client.PostAsJsonAsync("/api/chat/completions", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    #endregion

    #region POST /api/chat/completions/stream

    /// <summary>
    /// Tests that a streaming chat completion request returns SSE stream.
    /// </summary>
    [Fact]
    public async Task CreateChatCompletionStream_WithValidRequest_ReturnsSseStream()
    {
        // Arrange
        var request = new ChatCompletionRequest
        {
            Model = "gpt-4o",
            Messages =
            [
                new ChatMessage { Role = "user", Content = "Hello, how are you?" }
            ],
            Temperature = 0.7,
            Stream = true
        };

        // Act
        var response = await client.PostAsJsonAsync("/api/chat/completions/stream", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        response.Content.Headers.ContentType?.MediaType.Should().Be("text/event-stream");

        var content = await response.Content.ReadAsStringAsync();
        content.Should().Contain("data:");
        content.Should().Contain("[DONE]");
    }

    /// <summary>
    /// Tests that a streaming request returns chunks with correct structure.
    /// </summary>
    [Fact]
    public async Task CreateChatCompletionStream_ReturnsChunksWithCorrectStructure()
    {
        // Arrange
        var request = new ChatCompletionRequest
        {
            Model = "gpt-4o",
            Messages =
            [
                new ChatMessage { Role = "user", Content = "Say hello" }
            ],
            Stream = true
        };

        // Act
        var response = await client.PostAsJsonAsync("/api/chat/completions/stream", request);
        var content = await response.Content.ReadAsStringAsync();
        var lines = content.Split('\n', StringSplitOptions.RemoveEmptyEntries)
            .Where(l => l.StartsWith("data: ") && l != "data: [DONE]")
            .Select(l => l.Substring(6))
            .ToList();

        // Assert
        lines.Should().NotBeEmpty();
        foreach (var line in lines)
        {
            var chunk = JsonSerializer.Deserialize<ChatCompletionChunk>(line, new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower
            });
            chunk.Should().NotBeNull();
            chunk!.Id.Should().StartWith("chatcmpl-");
            chunk.Model.Should().Be("gpt-4o");
            chunk.Choices.Should().NotBeNullOrEmpty();
        }
    }

    #endregion

    #region GET /api/chat/costs/{tenantId}

    /// <summary>
    /// Tests that getting tenant costs returns cost data.
    /// </summary>
    [Fact]
    public async Task GetTenantCosts_WithValidTenantId_ReturnsCostData()
    {
        // Arrange
        var tenantId = Guid.NewGuid().ToString("N");

        // Act
        var response = await client.GetAsync($"/api/chat/costs/{tenantId}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var content = await response.Content.ReadFromJsonAsync<TenantCostResponse>();
        content.Should().NotBeNull();
        content!.TenantId.Should().Be(tenantId);
        content.Currency.Should().Be("USD");
        content.Breakdown.Should().NotBeNull();
        content.Period.Should().NotBeNull();
    }

    /// <summary>
    /// Tests that getting user costs returns cost data.
    /// </summary>
    [Fact]
    public async Task GetUserCosts_WithValidIds_ReturnsCostData()
    {
        // Arrange
        var tenantId = Guid.NewGuid().ToString("N");
        var userId = Guid.NewGuid().ToString("N");

        // Act
        var response = await client.GetAsync($"/api/chat/costs/{tenantId}/{userId}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var content = await response.Content.ReadFromJsonAsync<UserCostResponse>();
        content.Should().NotBeNull();
        content!.TenantId.Should().Be(tenantId);
        content.UserId.Should().Be(userId);
        content.Currency.Should().Be("USD");
        content.Breakdown.Should().NotBeNull();
    }

    #endregion
}
