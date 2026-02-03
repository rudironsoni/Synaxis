using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Moq;
using Xunit;

namespace Synaxis.InferenceGateway.Application.Tests.Optimization.Caching;

/// <summary>
/// Unit tests for IConversationCompressor implementations
/// Tests conversation compression strategies to reduce token usage
/// </summary>
public class ConversationCompressorTests
{
    private readonly Mock<IConversationCompressor> _mockCompressor;
    private readonly CancellationToken _cancellationToken;

    public ConversationCompressorTests()
    {
        _mockCompressor = new Mock<IConversationCompressor>();
        _cancellationToken = CancellationToken.None;
    }

    [Fact]
    public async Task Compress_ShortConversation_NoCompression()
    {
        // Arrange
        var messages = new List<ConversationMessage>
        {
            new ConversationMessage { Role = "system", Content = "You are a helpful assistant." },
            new ConversationMessage { Role = "user", Content = "Hello!" },
            new ConversationMessage { Role = "assistant", Content = "Hi! How can I help you today?" }
        };

        var threshold = 4000;
        var strategy = CompressionStrategy.Smart;

        _mockCompressor
            .Setup(x => x.CompressAsync(messages, threshold, strategy, _cancellationToken))
            .ReturnsAsync(new CompressionResult
            {
                CompressedMessages = messages,
                OriginalTokenCount = 25,
                CompressedTokenCount = 25,
                CompressionRatio = 1.0,
                StrategyUsed = strategy,
                WasCompressed = false
            });

        // Act
        var result = await _mockCompressor.Object.CompressAsync(
            messages, threshold, strategy, _cancellationToken);

        // Assert
        Assert.NotNull(result);
        Assert.False(result.WasCompressed);
        Assert.Equal(messages.Count, result.CompressedMessages.Count);
        Assert.Equal(25, result.OriginalTokenCount);
        Assert.Equal(25, result.CompressedTokenCount);
        Assert.Equal(1.0, result.CompressionRatio);
        
        _mockCompressor.Verify(
            x => x.CompressAsync(messages, threshold, strategy, _cancellationToken),
            Times.Once);
    }

    [Fact]
    public async Task Compress_ExceedsThreshold_SmartCompression()
    {
        // Arrange
        var messages = new List<ConversationMessage>();
        
        // Add system prompt
        messages.Add(new ConversationMessage { Role = "system", Content = "You are a helpful assistant." });
        
        // Add many user/assistant exchanges
        for (int i = 0; i < 20; i++)
        {
            messages.Add(new ConversationMessage 
            { 
                Role = "user", 
                Content = $"This is user message {i} with some content that takes up tokens." 
            });
            messages.Add(new ConversationMessage 
            { 
                Role = "assistant", 
                Content = $"This is assistant response {i} with detailed information." 
            });
        }

        var threshold = 500;
        var strategy = CompressionStrategy.Smart;

        // Expected: System prompt + recent messages + summary of middle section
        var compressedMessages = new List<ConversationMessage>
        {
            new ConversationMessage { Role = "system", Content = "You are a helpful assistant." },
            new ConversationMessage 
            { 
                Role = "system", 
                Content = "[Conversation summary: 30 messages about various topics compressed for context]" 
            }
        };
        
        // Add last 5 exchanges
        compressedMessages.AddRange(messages.Skip(messages.Count - 10).Take(10));

        _mockCompressor
            .Setup(x => x.CompressAsync(messages, threshold, strategy, _cancellationToken))
            .ReturnsAsync(new CompressionResult
            {
                CompressedMessages = compressedMessages,
                OriginalTokenCount = 1200,
                CompressedTokenCount = 450,
                CompressionRatio = 0.375,
                StrategyUsed = strategy,
                WasCompressed = true
            });

        // Act
        var result = await _mockCompressor.Object.CompressAsync(
            messages, threshold, strategy, _cancellationToken);

        // Assert
        Assert.NotNull(result);
        Assert.True(result.WasCompressed);
        Assert.True(result.CompressedMessages.Count < messages.Count);
        Assert.Equal(1200, result.OriginalTokenCount);
        Assert.Equal(450, result.CompressedTokenCount);
        Assert.True(result.CompressedTokenCount < threshold);
        Assert.True(result.CompressionRatio < 1.0);
        Assert.Equal(CompressionStrategy.Smart, result.StrategyUsed);
        
        _mockCompressor.Verify(
            x => x.CompressAsync(messages, threshold, strategy, _cancellationToken),
            Times.Once);
    }

    [Fact]
    public async Task Compress_ExceedsThreshold_TruncateOldest()
    {
        // Arrange
        var messages = new List<ConversationMessage>();
        
        for (int i = 0; i < 15; i++)
        {
            messages.Add(new ConversationMessage 
            { 
                Role = "user", 
                Content = $"User message {i}" 
            });
            messages.Add(new ConversationMessage 
            { 
                Role = "assistant", 
                Content = $"Assistant response {i}" 
            });
        }

        var threshold = 300;
        var strategy = CompressionStrategy.TruncateOldest;

        // Expected: Keep last 6 messages (3 exchanges)
        var compressedMessages = messages.Skip(messages.Count - 6).ToList();

        _mockCompressor
            .Setup(x => x.CompressAsync(messages, threshold, strategy, _cancellationToken))
            .ReturnsAsync(new CompressionResult
            {
                CompressedMessages = compressedMessages,
                OriginalTokenCount = 800,
                CompressedTokenCount = 250,
                CompressionRatio = 0.3125,
                StrategyUsed = strategy,
                WasCompressed = true
            });

        // Act
        var result = await _mockCompressor.Object.CompressAsync(
            messages, threshold, strategy, _cancellationToken);

        // Assert
        Assert.NotNull(result);
        Assert.True(result.WasCompressed);
        Assert.Equal(6, result.CompressedMessages.Count);
        Assert.Equal(800, result.OriginalTokenCount);
        Assert.Equal(250, result.CompressedTokenCount);
        Assert.True(result.CompressedTokenCount < threshold);
        Assert.Equal(CompressionStrategy.TruncateOldest, result.StrategyUsed);
        
        _mockCompressor.Verify(
            x => x.CompressAsync(messages, threshold, strategy, _cancellationToken),
            Times.Once);
    }

    [Fact]
    public async Task Compress_SystemPrompts_Preserved()
    {
        // Arrange
        var messages = new List<ConversationMessage>
        {
            new ConversationMessage { Role = "system", Content = "You are a helpful assistant specialized in code." },
            new ConversationMessage { Role = "system", Content = "Always format code with syntax highlighting." }
        };
        
        // Add many messages
        for (int i = 0; i < 20; i++)
        {
            messages.Add(new ConversationMessage { Role = "user", Content = $"Message {i}" });
            messages.Add(new ConversationMessage { Role = "assistant", Content = $"Response {i}" });
        }

        var threshold = 400;
        var strategy = CompressionStrategy.Smart;

        // Expected: System prompts always preserved
        var compressedMessages = new List<ConversationMessage>
        {
            messages[0], // First system prompt
            messages[1], // Second system prompt
        };
        compressedMessages.AddRange(messages.Skip(messages.Count - 8)); // Last 4 exchanges

        _mockCompressor
            .Setup(x => x.CompressAsync(messages, threshold, strategy, _cancellationToken))
            .ReturnsAsync(new CompressionResult
            {
                CompressedMessages = compressedMessages,
                OriginalTokenCount = 1000,
                CompressedTokenCount = 380,
                CompressionRatio = 0.38,
                StrategyUsed = strategy,
                WasCompressed = true
            });

        // Act
        var result = await _mockCompressor.Object.CompressAsync(
            messages, threshold, strategy, _cancellationToken);

        // Assert
        Assert.NotNull(result);
        Assert.True(result.WasCompressed);
        
        // Verify system prompts are at the beginning
        Assert.Equal("system", result.CompressedMessages[0].Role);
        Assert.Equal("system", result.CompressedMessages[1].Role);
        Assert.Equal("You are a helpful assistant specialized in code.", result.CompressedMessages[0].Content);
        Assert.Equal("Always format code with syntax highlighting.", result.CompressedMessages[1].Content);
        
        _mockCompressor.Verify(
            x => x.CompressAsync(messages, threshold, strategy, _cancellationToken),
            Times.Once);
    }

    [Fact]
    public async Task Compress_RecentMessages_Preserved()
    {
        // Arrange
        var messages = new List<ConversationMessage>();
        
        for (int i = 0; i < 20; i++)
        {
            messages.Add(new ConversationMessage { Role = "user", Content = $"User message {i}" });
            messages.Add(new ConversationMessage { Role = "assistant", Content = $"Response {i}" });
        }

        var threshold = 400;
        var strategy = CompressionStrategy.Smart;

        // Most recent 3 exchanges should be preserved
        var recentMessages = messages.Skip(messages.Count - 6).ToList();

        _mockCompressor
            .Setup(x => x.CompressAsync(messages, threshold, strategy, _cancellationToken))
            .ReturnsAsync(new CompressionResult
            {
                CompressedMessages = recentMessages,
                OriginalTokenCount = 1000,
                CompressedTokenCount = 350,
                CompressionRatio = 0.35,
                StrategyUsed = strategy,
                WasCompressed = true
            });

        // Act
        var result = await _mockCompressor.Object.CompressAsync(
            messages, threshold, strategy, _cancellationToken);

        // Assert
        Assert.NotNull(result);
        Assert.True(result.WasCompressed);
        
        // Verify recent messages are preserved
        Assert.Equal("User message 19", result.CompressedMessages.Last().Content);
        Assert.Equal("user", result.CompressedMessages[result.CompressedMessages.Count - 2].Role);
        
        _mockCompressor.Verify(
            x => x.CompressAsync(messages, threshold, strategy, _cancellationToken),
            Times.Once);
    }

    [Fact]
    public async Task Compress_EmptyInput_HandlesGracefully()
    {
        // Arrange
        var messages = new List<ConversationMessage>();
        var threshold = 4000;
        var strategy = CompressionStrategy.Smart;

        _mockCompressor
            .Setup(x => x.CompressAsync(messages, threshold, strategy, _cancellationToken))
            .ReturnsAsync(new CompressionResult
            {
                CompressedMessages = messages,
                OriginalTokenCount = 0,
                CompressedTokenCount = 0,
                CompressionRatio = 1.0,
                StrategyUsed = strategy,
                WasCompressed = false
            });

        // Act
        var result = await _mockCompressor.Object.CompressAsync(
            messages, threshold, strategy, _cancellationToken);

        // Assert
        Assert.NotNull(result);
        Assert.False(result.WasCompressed);
        Assert.Empty(result.CompressedMessages);
        Assert.Equal(0, result.OriginalTokenCount);
        Assert.Equal(0, result.CompressedTokenCount);
    }

    [Fact]
    public void EstimateTokens_AccurateApproximation()
    {
        // Arrange
        var message = "This is a test message with approximately 10 tokens in it.";
        
        _mockCompressor
            .Setup(x => x.EstimateTokens(message))
            .Returns(10);

        // Act
        var tokenCount = _mockCompressor.Object.EstimateTokens(message);

        // Assert
        Assert.Equal(10, tokenCount);
        Assert.True(tokenCount > 0);
        
        _mockCompressor.Verify(x => x.EstimateTokens(message), Times.Once);
    }

    [Fact]
    public void EstimateTokens_EmptyString_ReturnsZero()
    {
        // Arrange
        var message = string.Empty;
        
        _mockCompressor
            .Setup(x => x.EstimateTokens(message))
            .Returns(0);

        // Act
        var tokenCount = _mockCompressor.Object.EstimateTokens(message);

        // Assert
        Assert.Equal(0, tokenCount);
    }

    [Fact]
    public async Task SmartCompression_MiddleSection_Summarized()
    {
        // Arrange
        var messages = new List<ConversationMessage>
        {
            new ConversationMessage { Role = "system", Content = "You are a helpful assistant." }
        };
        
        // Add old messages (should be summarized)
        for (int i = 0; i < 10; i++)
        {
            messages.Add(new ConversationMessage { Role = "user", Content = $"Old user message {i}" });
            messages.Add(new ConversationMessage { Role = "assistant", Content = $"Old response {i}" });
        }
        
        // Add recent messages (should be kept)
        for (int i = 0; i < 3; i++)
        {
            messages.Add(new ConversationMessage { Role = "user", Content = $"Recent user message {i}" });
            messages.Add(new ConversationMessage { Role = "assistant", Content = $"Recent response {i}" });
        }

        var threshold = 500;
        var strategy = CompressionStrategy.Smart;

        var compressedMessages = new List<ConversationMessage>
        {
            messages[0], // System prompt
            new ConversationMessage 
            { 
                Role = "system", 
                Content = "[Summary: Previous conversation covered topics A, B, and C]" 
            }
        };
        compressedMessages.AddRange(messages.Skip(messages.Count - 6)); // Last 3 exchanges

        _mockCompressor
            .Setup(x => x.CompressAsync(messages, threshold, strategy, _cancellationToken))
            .ReturnsAsync(new CompressionResult
            {
                CompressedMessages = compressedMessages,
                OriginalTokenCount = 800,
                CompressedTokenCount = 450,
                CompressionRatio = 0.5625,
                StrategyUsed = strategy,
                WasCompressed = true
            });

        // Act
        var result = await _mockCompressor.Object.CompressAsync(
            messages, threshold, strategy, _cancellationToken);

        // Assert
        Assert.NotNull(result);
        Assert.True(result.WasCompressed);
        
        // Verify summary message exists
        var summaryMessage = result.CompressedMessages.FirstOrDefault(m => 
            m.Role == "system" && m.Content.Contains("[Summary:"));
        Assert.NotNull(summaryMessage);
        Assert.Contains("Summary", summaryMessage.Content);
        
        // Verify recent messages are preserved
        Assert.Contains(result.CompressedMessages, m => m.Content == "Recent user message 2");
        
        _mockCompressor.Verify(
            x => x.CompressAsync(messages, threshold, strategy, _cancellationToken),
            Times.Once);
    }

    [Fact]
    public async Task TruncateOldest_KeepsMostRecent()
    {
        // Arrange
        var messages = new List<ConversationMessage>();
        
        for (int i = 0; i < 10; i++)
        {
            messages.Add(new ConversationMessage { Role = "user", Content = $"Message {i}" });
            messages.Add(new ConversationMessage { Role = "assistant", Content = $"Response {i}" });
        }

        var threshold = 200;
        var strategy = CompressionStrategy.TruncateOldest;

        // Keep last 4 messages (2 exchanges)
        var compressedMessages = messages.Skip(16).ToList();

        _mockCompressor
            .Setup(x => x.CompressAsync(messages, threshold, strategy, _cancellationToken))
            .ReturnsAsync(new CompressionResult
            {
                CompressedMessages = compressedMessages,
                OriginalTokenCount = 600,
                CompressedTokenCount = 180,
                CompressionRatio = 0.3,
                StrategyUsed = strategy,
                WasCompressed = true
            });

        // Act
        var result = await _mockCompressor.Object.CompressAsync(
            messages, threshold, strategy, _cancellationToken);

        // Assert
        Assert.NotNull(result);
        Assert.True(result.WasCompressed);
        Assert.Equal(4, result.CompressedMessages.Count);
        
        // Verify oldest messages are removed
        Assert.DoesNotContain(result.CompressedMessages, m => m.Content == "Message 0");
        Assert.DoesNotContain(result.CompressedMessages, m => m.Content == "Message 1");
        
        // Verify most recent are kept
        Assert.Contains(result.CompressedMessages, m => m.Content == "Message 8");
        Assert.Contains(result.CompressedMessages, m => m.Content == "Message 9");
    }

    [Fact]
    public async Task Compress_WithCancellationToken_RespectsCancellation()
    {
        // Arrange
        var messages = new List<ConversationMessage>
        {
            new ConversationMessage { Role = "user", Content = "Hello" }
        };
        
        var threshold = 4000;
        var strategy = CompressionStrategy.Smart;
        var cts = new CancellationTokenSource();
        cts.Cancel();

        _mockCompressor
            .Setup(x => x.CompressAsync(messages, threshold, strategy, cts.Token))
            .ThrowsAsync(new OperationCanceledException());

        // Act & Assert
        await Assert.ThrowsAsync<OperationCanceledException>(async () =>
        {
            await _mockCompressor.Object.CompressAsync(
                messages, threshold, strategy, cts.Token);
        });
    }

    [Fact]
    public void EstimateTokens_LongText_ScalesLinearly()
    {
        // Arrange
        var shortText = "Hello world";
        var longText = new string('x', 1000);
        
        _mockCompressor.Setup(x => x.EstimateTokens(shortText)).Returns(3);
        _mockCompressor.Setup(x => x.EstimateTokens(longText)).Returns(250);

        // Act
        var shortTokens = _mockCompressor.Object.EstimateTokens(shortText);
        var longTokens = _mockCompressor.Object.EstimateTokens(longText);

        // Assert
        Assert.True(longTokens > shortTokens);
        Assert.Equal(3, shortTokens);
        Assert.Equal(250, longTokens);
    }
}

/// <summary>
/// Represents a message in a conversation
/// </summary>
public class ConversationMessage
{
    public string Role { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;
    public DateTimeOffset Timestamp { get; set; } = DateTimeOffset.UtcNow;
}

/// <summary>
/// Result of a compression operation
/// </summary>
public class CompressionResult
{
    public List<ConversationMessage> CompressedMessages { get; set; } = new();
    public int OriginalTokenCount { get; set; }
    public int CompressedTokenCount { get; set; }
    public double CompressionRatio { get; set; }
    public CompressionStrategy StrategyUsed { get; set; }
    public bool WasCompressed { get; set; }
}

/// <summary>
/// Compression strategy options
/// </summary>
public enum CompressionStrategy
{
    None,
    TruncateOldest,
    Smart,
    Summarize
}

/// <summary>
/// Interface for conversation compression
/// </summary>
public interface IConversationCompressor
{
    Task<CompressionResult> CompressAsync(
        List<ConversationMessage> messages,
        int tokenThreshold,
        CompressionStrategy strategy,
        CancellationToken cancellationToken);

    int EstimateTokens(string text);
}
