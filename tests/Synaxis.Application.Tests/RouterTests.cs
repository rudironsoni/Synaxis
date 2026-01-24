using Microsoft.Extensions.AI;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using Synaxis.Application.ChatClients;

namespace Synaxis.Application.Tests;

public class RouterTests
{
    private readonly Mock<IChatClient> _groqMock;
    private readonly Mock<IChatClient> _geminiMock;
    private readonly RoutingChatClient _router;

    public RouterTests()
    {
        _groqMock = new Mock<IChatClient>();
        _geminiMock = new Mock<IChatClient>();
        
        // RoutingChatClient uses keyed services, but we can just pass them in the constructor
        _router = new RoutingChatClient(_groqMock.Object, _geminiMock.Object);
    }

    [Fact]
    public async Task GetResponseAsync_WithGeminiModel_RoutesToGemini()
    {
        // Arrange
        var messages = new[] { new ChatMessage(ChatRole.User, "Hello") };
        var options = new ChatOptions { ModelId = "gemini-1.5-pro" };
        var expectedResponse = new ChatResponse(new ChatMessage(ChatRole.Assistant, "Gemini Response"));
        
        _geminiMock.Setup(c => c.GetResponseAsync(messages, options, It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedResponse);

        // Act
        var response = await _router.GetResponseAsync(messages, options);

        // Assert
        Assert.Equal("Gemini Response", response.Text);
        _geminiMock.Verify(c => c.GetResponseAsync(messages, options, It.IsAny<CancellationToken>()), Times.Once);
        _groqMock.Verify(c => c.GetResponseAsync(It.IsAny<IEnumerable<ChatMessage>>(), It.IsAny<ChatOptions>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task GetResponseAsync_WithLlamaModel_RoutesToGroq()
    {
        // Arrange
        var messages = new[] { new ChatMessage(ChatRole.User, "Hello") };
        var options = new ChatOptions { ModelId = "llama3-70b" };
        var expectedResponse = new ChatResponse(new ChatMessage(ChatRole.Assistant, "Groq Response"));
        
        _groqMock.Setup(c => c.GetResponseAsync(messages, options, It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedResponse);

        // Act
        var response = await _router.GetResponseAsync(messages, options);

        // Assert
        Assert.Equal("Groq Response", response.Text);
        _groqMock.Verify(c => c.GetResponseAsync(messages, options, It.IsAny<CancellationToken>()), Times.Once);
        _geminiMock.Verify(c => c.GetResponseAsync(It.IsAny<IEnumerable<ChatMessage>>(), It.IsAny<ChatOptions>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task GetResponseAsync_WithNullModel_RoutesToGeminiDefault()
    {
        // Arrange
        var messages = new[] { new ChatMessage(ChatRole.User, "Hello") };
        var expectedResponse = new ChatResponse(new ChatMessage(ChatRole.Assistant, "Default Gemini Response"));
        
        _geminiMock.Setup(c => c.GetResponseAsync(messages, null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedResponse);

        // Act
        var response = await _router.GetResponseAsync(messages, null);

        // Assert
        Assert.Equal("Default Gemini Response", response.Text);
        _geminiMock.Verify(c => c.GetResponseAsync(messages, null, It.IsAny<CancellationToken>()), Times.Once);
    }
    
    [Fact]
    public void Dispose_DisposesBothClients()
    {
        // Act
        _router.Dispose();

        // Assert
        _groqMock.Verify(c => c.Dispose(), Times.Once);
        _geminiMock.Verify(c => c.Dispose(), Times.Once);
    }

    [Fact]
    public void Metadata_ReturnsCorrectMetadata()
    {
        // Act
        var metadata = _router.Metadata;

        // Assert
        Assert.NotNull(metadata);
    }

    [Fact]
    public void GetService_ReturnsMetadata()
    {
        // Act
        var result = _router.GetService(typeof(ChatClientMetadata));

        // Assert
        Assert.IsType<ChatClientMetadata>(result);
    }

    [Fact]
    public void GetService_ReturnsNullForUnknownType()
    {
        // Act
        var result = _router.GetService(typeof(string));

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public async Task GetStreamingResponseAsync_RoutesToGemini()
    {
        // Arrange
        var messages = new[] { new ChatMessage(ChatRole.User, "Hello") };
        var options = new ChatOptions { ModelId = "gemini-1.5-pro" };
        var update = new ChatResponseUpdate { Role = ChatRole.Assistant };
        update.Contents.Add(new TextContent("Part 1"));
        var updates = new[] { update }.ToAsyncEnumerable();
        
        _geminiMock.Setup(c => c.GetStreamingResponseAsync(messages, options, It.IsAny<CancellationToken>()))
            .Returns(updates);

        // Act
        var response = _router.GetStreamingResponseAsync(messages, options);
        var result = await response.ToListAsync();

        // Assert
        Assert.Single(result);
        Assert.Equal("Part 1", result[0].Text);
        _geminiMock.Verify(c => c.GetStreamingResponseAsync(messages, options, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task GetStreamingResponseAsync_RoutesToGroq()
    {
        // Arrange
        var messages = new[] { new ChatMessage(ChatRole.User, "Hello") };
        var options = new ChatOptions { ModelId = "llama3-70b" };
        var update = new ChatResponseUpdate { Role = ChatRole.Assistant };
        update.Contents.Add(new TextContent("Groq Part"));
        var updates = new[] { update }.ToAsyncEnumerable();
        
        _groqMock.Setup(c => c.GetStreamingResponseAsync(messages, options, It.IsAny<CancellationToken>()))
            .Returns(updates);

        // Act
        var response = _router.GetStreamingResponseAsync(messages, options);
        var result = await response.ToListAsync();

        // Assert
        Assert.Single(result);
        Assert.Equal("Groq Part", result[0].Text);
        _groqMock.Verify(c => c.GetStreamingResponseAsync(messages, options, It.IsAny<CancellationToken>()), Times.Once);
    }
}

public static class AsyncEnumerableExtensions
{
    public static async Task<List<T>> ToListAsync<T>(this IAsyncEnumerable<T> source)
    {
        var list = new List<T>();
        await foreach (var item in source)
        {
            list.Add(item);
        }
        return list;
    }

    public static async IAsyncEnumerable<T> ToAsyncEnumerable<T>(this IEnumerable<T> source)
    {
        foreach (var item in source)
        {
            yield return item;
            await Task.Yield();
        }
    }
}
