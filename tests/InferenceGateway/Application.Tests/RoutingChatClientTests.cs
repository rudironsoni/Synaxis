using Microsoft.Extensions.AI;
using Moq;
using Synaxis.InferenceGateway.Application.ChatClients;

namespace Synaxis.InferenceGateway.Application.Tests;

public class RoutingChatClientTests
{
    private readonly Mock<IChatClient> _groqMock;
    private readonly Mock<IChatClient> _geminiMock;
    private readonly RoutingChatClient _sut;

    public RoutingChatClientTests()
    {
        _groqMock = new Mock<IChatClient>();
        _geminiMock = new Mock<IChatClient>();
        _sut = new RoutingChatClient(_groqMock.Object, _geminiMock.Object);
    }

    [Fact]
    public async Task CompleteAsync_GeminiModel_UsesGeminiClient()
    {
        // Arrange
        var messages = new List<ChatMessage> { new(ChatRole.User, "Hello") };
        var options = new ChatOptions { ModelId = "gemini-1.5-flash" };
        var expectedResponse = new ChatResponse(new ChatMessage(ChatRole.Assistant, "Gemini response"));
        
        _geminiMock.Setup(x => x.GetResponseAsync(messages, options, It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedResponse);

        // Act
        var response = await _sut.GetResponseAsync(messages, options);

        // Assert
        Assert.Equal(expectedResponse, response);
        _geminiMock.Verify(x => x.GetResponseAsync(messages, options, It.IsAny<CancellationToken>()), Times.Once);
        _groqMock.Verify(x => x.GetResponseAsync(It.IsAny<IEnumerable<ChatMessage>>(), It.IsAny<ChatOptions>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task CompleteAsync_LlamaModel_UsesGroqClient()
    {
        // Arrange
        var messages = new List<ChatMessage> { new(ChatRole.User, "Hello") };
        var options = new ChatOptions { ModelId = "llama-3-70b" };
        var expectedResponse = new ChatResponse(new ChatMessage(ChatRole.Assistant, "Groq response"));
        
        _groqMock.Setup(x => x.GetResponseAsync(messages, options, It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedResponse);

        // Act
        var response = await _sut.GetResponseAsync(messages, options);

        // Assert
        Assert.Equal(expectedResponse, response);
        _groqMock.Verify(x => x.GetResponseAsync(messages, options, It.IsAny<CancellationToken>()), Times.Once);
        _geminiMock.Verify(x => x.GetResponseAsync(It.IsAny<IEnumerable<ChatMessage>>(), It.IsAny<ChatOptions>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task CompleteAsync_Fallback_UsesGemini()
    {
        // Arrange
        var messages = new List<ChatMessage> { new(ChatRole.User, "Hello") };
        var options = new ChatOptions { ModelId = "unknown-model" };
        var expectedResponse = new ChatResponse(new ChatMessage(ChatRole.Assistant, "Gemini fallback response"));
        
        _geminiMock.Setup(x => x.GetResponseAsync(messages, options, It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedResponse);

        // Act
        var response = await _sut.GetResponseAsync(messages, options);

        // Assert
        Assert.Equal(expectedResponse, response);
        _geminiMock.Verify(x => x.GetResponseAsync(messages, options, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task CompleteStreamingAsync_FollowsRoutingRules()
    {
        // Arrange
        var messages = new List<ChatMessage> { new(ChatRole.User, "Hello") };
        var options = new ChatOptions { ModelId = "llama-3-8b" };
        var expectedUpdate = new ChatResponseUpdate();
        expectedUpdate.Contents.Add(new TextContent("Groq stream"));
        
        _groqMock.Setup(x => x.GetStreamingResponseAsync(messages, options, It.IsAny<CancellationToken>()))
            .Returns(new[] { expectedUpdate }.ToAsyncEnumerable());

        // Act
        var updates = _sut.GetStreamingResponseAsync(messages, options);
        var list = await updates.ToListAsync();

        // Assert
        Assert.Single(list);
        Assert.Equal(expectedUpdate.Text, list[0].Text);
        _groqMock.Verify(x => x.GetStreamingResponseAsync(messages, options, It.IsAny<CancellationToken>()), Times.Once);
    }
}
