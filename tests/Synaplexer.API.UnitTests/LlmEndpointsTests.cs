using System.Net;
using System.Net.Http.Json;
using Synaplexer.Application.Commands;
using Synaplexer.Application.Queries;
using Synaplexer.Application.Dtos;
using Synaplexer.API.Endpoints;
using FluentAssertions;
using NSubstitute;
using Xunit;

namespace Synaplexer.Api.Tests;

public class LlmEndpointsTests : IClassFixture<LlmApiFactory>
{
    private readonly LlmApiFactory _factory;
    private readonly HttpClient _client;

    public LlmEndpointsTests(LlmApiFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task ChatCompletions_ReturnsSuccess_WhenRequestIsValid()
    {
        // Arrange
        var request = new ChatCompletionRequest(
            Model: "gpt-4",
            Messages: new[] { new ChatMessage("user", "Hello") },
            Temperature: 0.7f,
            MaxTokens: 100,
            Stream: false
        );

        var expectedResult = new ChatCompletionResult(
            Content: "Hi there!",
            Model: "gpt-4",
            UsageTokens: 10,
            FinishReason: "stop"
        );

        _factory.Mediator.Send(Arg.Any<ChatCompletionCommand>(), Arg.Any<CancellationToken>())
            .Returns(expectedResult);

        // Act
        var response = await _client.PostAsJsonAsync("/v1/chat/completions", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<TestChatResponse>();
        result!.model.Should().Be("gpt-4");
        result.choices[0].message.content.Should().Be("Hi there!");
    }

    [Fact]
    public async Task ChatCompletions_ReturnsSuccess_WhenStreamIsTrue()
    {
        // Arrange
        var request = new ChatCompletionRequest(
            Model: "gpt-4",
            Messages: new[] { new ChatMessage("user", "Hello") },
            Stream: true
        );

        var expectedResult = new ChatCompletionResult(
            Content: "Hi there!",
            Model: "gpt-4",
            UsageTokens: 10,
            FinishReason: "stop"
        );

        _factory.Mediator.Send(Arg.Any<ChatCompletionCommand>(), Arg.Any<CancellationToken>())
            .Returns(expectedResult);

        // Act
        var response = await _client.PostAsJsonAsync("/v1/chat/completions", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        response.Content.Headers.ContentType!.MediaType.Should().Be("text/event-stream");
        var content = await response.Content.ReadAsStringAsync();
        content.Should().Contain("data: {");
        content.Should().Contain("data: [DONE]");
    }

    [Fact]
    public async Task ChatCompletions_ReturnsValidationProblem_WhenModelIsMissing()
    {
        // Arrange
        var request = new ChatCompletionRequest(
            Model: "",
            Messages: new[] { new ChatMessage("user", "Hello") }
        );

        // Act
        var response = await _client.PostAsJsonAsync("/v1/chat/completions", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task ChatCompletions_ReturnsValidationProblem_WhenMessagesAreMissing()
    {
        // Arrange
        var request = new ChatCompletionRequest(
            Model: "gpt-4",
            Messages: new ChatMessage[0]
        );

        // Act
        var response = await _client.PostAsJsonAsync("/v1/chat/completions", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task InitializeProvider_ReturnsOk_WhenSuccessful()
    {
        // Arrange
        _factory.Mediator.Send(Arg.Any<InitializeProviderCommand>(), Arg.Any<CancellationToken>())
            .Returns(true);

        // Act
        var response = await _client.PostAsync("/api/providers/openai/initialize", null);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task InitializeProvider_ReturnsBadRequest_WhenFailed()
    {
        // Arrange
        _factory.Mediator.Send(Arg.Any<InitializeProviderCommand>(), Arg.Any<CancellationToken>())
            .Returns(false);

        // Act
        var response = await _client.PostAsync("/api/providers/openai/initialize", null);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task ListModels_ReturnsSuccess()
    {
        // Arrange
        var expectedModels = new[] 
        { 
            new ModelInfo("gpt-4", "GPT-4", "OpenAI", 8192),
            new ModelInfo("claude-3", "Claude 3", "Anthropic", 200000)
        };
        _factory.Mediator.Send(Arg.Any<ListAvailableModelsQuery>(), Arg.Any<CancellationToken>())
            .Returns(expectedModels);

        // Act
        var response = await _client.GetAsync("/v1/models");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<System.Text.Json.JsonDocument>();
        var data = result!.RootElement.GetProperty("data");
        data.GetArrayLength().Should().Be(2);
    }

    [Fact]
    public async Task GetProviderStatus_ReturnsStatus()
    {
        // Arrange
        var status = new ProviderStatusDto("openai", true, "Available", DateTime.UtcNow);
        _factory.Mediator.Send(Arg.Any<GetProviderStatusQuery>(), Arg.Any<CancellationToken>())
            .Returns(status);

        // Act
        var response = await _client.GetAsync("/api/providers/status/openai");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<ProviderStatusDto>();
        result!.ProviderName.Should().Be("openai");
        result.IsHealthy.Should().BeTrue();
    }

    [Fact]
    public async Task ChatCompletions_ReturnsValidationProblem_WhenMessagesAreNull()
    {
        // Arrange
        var request = new ChatCompletionRequest(
            Model: "gpt-4",
            Messages: null!
        );

        // Act
        var response = await _client.PostAsJsonAsync("/v1/chat/completions", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task ChatCompletions_ReturnsValidationProblem_WhenMessagesAreEmpty()
    {
        // Arrange
        var request = new ChatCompletionRequest(
            Model: "gpt-4",
            Messages: new ChatMessage[0]
        );

        // Act
        var response = await _client.PostAsJsonAsync("/v1/chat/completions", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    private record TestChatResponse(string model, TestChoice[] choices);
    private record TestChoice(TestMessage message);
    private record TestMessage(string content);
}
