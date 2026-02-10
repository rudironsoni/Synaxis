using Microsoft.Extensions.AI;

namespace Synaxis.Common.Tests;

/// <summary>
/// Static helper for generating test data for Token Optimization testing.
/// Provides factory methods for creating common test objects with sensible defaults.
/// </summary>
public static class TestDataFactory
{
    /// <summary>
    /// Creates a user chat message with optional custom text.
    /// </summary>
    /// <returns></returns>
    public static ChatMessage CreateUserMessage(string text = "Test query")
        => new(ChatRole.User, text);

    /// <summary>
    /// Creates an assistant chat message with optional custom text.
    /// </summary>
    /// <returns></returns>
    public static ChatMessage CreateAssistantMessage(string text = "Test response")
        => new(ChatRole.Assistant, text);

    /// <summary>
    /// Creates chat options with default settings.
    /// </summary>
    /// <returns></returns>
    public static ChatOptions CreateChatOptions(
        string model = "gpt-4",
        float? temperature = 0.7f)
        => new() { ModelId = model, Temperature = temperature };

    /// <summary>
    /// Creates a token optimization configuration with sensible defaults.
    /// Use the configure action to override specific settings.
    /// </summary>
    /// <returns></returns>
    public static TokenOptimizationOptions CreateOptimizationConfig(
        Action<TokenOptimizationOptions>? configure = null)
    {
        var config = new TokenOptimizationOptions
        {
            Enabled = true,
            SemanticCacheEnabled = true,
            SemanticSimilarityThreshold = 0.85f,
            CompressionEnabled = true,
            CompressionStrategy = "SmartCompression",
            MaxTokensBeforeCompression = 4000,
            SessionAffinityEnabled = true,
            SessionAffinityTtlHours = 24,
            DeduplicationEnabled = true,
            DeduplicationTtlSeconds = 30,
        };
        configure?.Invoke(config);
        return config;
    }

    /// <summary>
    /// Creates a conversation with alternating user and assistant messages.
    /// </summary>
    /// <returns></returns>
    public static IEnumerable<ChatMessage> CreateConversation(int messageCount)
    {
        for (int i = 0; i < messageCount; i++)
        {
            yield return new ChatMessage(
                i % 2 == 0 ? ChatRole.User : ChatRole.Assistant,
                $"Message {i}: {Guid.NewGuid()}");
        }
    }

    /// <summary>
    /// Creates a test embedding with the specified dimensions.
    /// Returns a float array suitable for embedding operations.
    /// </summary>
    /// <returns></returns>
    public static float[] CreateTestEmbedding(int dimensions = 768)
    {
        var values = new float[dimensions];
        var random = new Random(42); // Fixed seed for reproducibility
        for (int i = 0; i < dimensions; i++)
        {
            values[i] = (float)random.NextDouble();
        }
        return values;
    }
}

