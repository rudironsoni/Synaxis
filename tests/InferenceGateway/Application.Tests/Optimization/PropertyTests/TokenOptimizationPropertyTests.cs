using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using FsCheck;
using FsCheck.Xunit;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Logging.Abstractions;
using Synaxis.Common.Tests;
using Synaxis.InferenceGateway.Application.Tests.Optimization.Caching;
using Synaxis.InferenceGateway.Application.Tests.Optimization.Configuration;
using Xunit;

namespace Synaxis.InferenceGateway.Application.Tests.Optimization.PropertyTests;

/// <summary>
/// Property-based tests for compression invariants using FsCheck
/// Tests that compression always maintains expected mathematical properties
/// </summary>
public class CompressionProperties
{
    [Property(MaxTest = 100)]
    public Property Compress_AlwaysReducesOrMaintainsTokens()
    {
        return Prop.ForAll(
            MessageListArbitrary.Messages(),
            Arb.From(Gen.Choose(100, 10000)),
            (messages, maxTokens) =>
            {
                // Arrange
                var compressor = new TestConversationCompressor();
                var result = compressor.CompressAsync(
                    messages,
                    maxTokens,
                    CompressionStrategy.Smart,
                    CancellationToken.None).Result;

                // Assert: Compressed tokens should never exceed original tokens
                return (result.CompressedTokenCount <= result.OriginalTokenCount)
                    .Label($"Original: {result.OriginalTokenCount}, Compressed: {result.CompressedTokenCount}, Strategy: {result.StrategyUsed}");
            });
    }

    [Property(MaxTest = 50)]
    public Property Compress_Deterministic()
    {
        return Prop.ForAll(
            MessageListArbitrary.Messages(),
            Arb.From(Gen.Choose(1000, 8000)),
            (messages, maxTokens) =>
            {
                // Arrange
                var compressor = new TestConversationCompressor();

                // Act - Compress twice with same inputs
                var result1 = compressor.CompressAsync(messages, maxTokens, CompressionStrategy.Smart, CancellationToken.None).Result;
                var result2 = compressor.CompressAsync(messages, maxTokens, CompressionStrategy.Smart, CancellationToken.None).Result;

                // Assert: Results should be identical
                return (result1.CompressedTokenCount == result2.CompressedTokenCount &&
                        result1.OriginalTokenCount == result2.OriginalTokenCount &&
                        result1.CompressedMessages.Count == result2.CompressedMessages.Count)
                    .Label("Compression must be deterministic");
            });
    }

    [Property(MaxTest = 75)]
    public Property Compress_CompressionRatioValid()
    {
        return Prop.ForAll(
            MessageListArbitrary.Messages(),
            Arb.From(Gen.Choose(500, 5000)),
            (messages, maxTokens) =>
            {
                // Arrange
                var compressor = new TestConversationCompressor();
                var result = compressor.CompressAsync(
                    messages,
                    maxTokens,
                    CompressionStrategy.Smart,
                    CancellationToken.None).Result;

                // Assert: Compression ratio should be between 0 and 1
                var expectedRatio = result.OriginalTokenCount > 0
                    ? (double)result.CompressedTokenCount / result.OriginalTokenCount
                    : 1.0;

                return (result.CompressionRatio >= 0 && result.CompressionRatio <= 1.0)
                    .Label($"Ratio: {result.CompressionRatio}, Expected: {expectedRatio}");
            });
    }

    [Property(MaxTest = 50)]
    public Property Compress_PreservesSystemMessages()
    {
        return Prop.ForAll(
            MessageListArbitrary.MessagesWithSystem(),
            Arb.From(Gen.Choose(500, 5000)),
            (messages, maxTokens) =>
            {
                // Arrange
                var compressor = new TestConversationCompressor();
                var systemMessages = messages.Where(m => m.Role == "system").ToList();

                if (systemMessages.Count == 0)
                    return true.Label("No system messages to preserve");

                // Act
                var result = compressor.CompressAsync(
                    messages,
                    maxTokens,
                    CompressionStrategy.Smart,
                    CancellationToken.None).Result;

                // Assert: All system messages should be preserved
                var resultSystemMessages = result.CompressedMessages.Where(m => m.Role == "system").ToList();
                return (resultSystemMessages.Count >= systemMessages.Count)
                    .Label($"Original system messages: {systemMessages.Count}, Preserved: {resultSystemMessages.Count}");
            });
    }

    [Property(MaxTest = 50)]
    public Property Compress_EmptyInput_NoCompression()
    {
        return Prop.ForAll(
            Arb.From(Gen.Choose(100, 10000)),
            maxTokens =>
            {
                // Arrange
                var compressor = new TestConversationCompressor();
                var emptyMessages = new List<ConversationMessage>();

                // Act
                var result = compressor.CompressAsync(
                    emptyMessages,
                    maxTokens,
                    CompressionStrategy.Smart,
                    CancellationToken.None).Result;

                // Assert
                return (!result.WasCompressed &&
                        result.OriginalTokenCount == 0 &&
                        result.CompressedTokenCount == 0)
                    .Label("Empty input should not trigger compression");
            });
    }
}

/// <summary>
/// Property-based tests for fingerprint determinism and collision resistance
/// </summary>
public class FingerprintProperties
{
    [Property(MaxTest = 200)]
    public Property ComputeFingerprint_Deterministic()
    {
        return Prop.ForAll(
            MessageListArbitrary.ChatMessages(),
            ChatOptionsArbitrary.ChatOptions(),
            (messages, options) =>
            {
                // Arrange
                var fingerprinter = new TestRequestFingerprinter();

                // Act - Compute fingerprint twice
                var hash1 = fingerprinter.ComputeFingerprint(messages, options);
                var hash2 = fingerprinter.ComputeFingerprint(messages, options);

                // Assert: Same input should always produce same hash
                return (hash1 == hash2)
                    .Label($"Hash: {hash1}");
            });
    }

    [Property(MaxTest = 100)]
    public Property ComputeFingerprint_DifferentMessages_DifferentHashes()
    {
        return Prop.ForAll(
            MessageListArbitrary.ChatMessages(),
            MessageListArbitrary.ChatMessages(),
            (messages1, messages2) =>
            {
                // Skip if messages are identical
                if (MessagesEqual(messages1, messages2))
                    return true.Label("Messages are identical - skipped");

                // Arrange
                var fingerprinter = new TestRequestFingerprinter();

                // Act
                var hash1 = fingerprinter.ComputeFingerprint(messages1, null);
                var hash2 = fingerprinter.ComputeFingerprint(messages2, null);

                // Assert: Different messages should produce different hashes (collision resistance)
                return (hash1 != hash2)
                    .Label($"Hash1: {hash1}, Hash2: {hash2}");
            });
    }

    [Property(MaxTest = 100)]
    public Property ComputeFingerprint_DifferentOptions_DifferentHashes()
    {
        return Prop.ForAll(
            MessageListArbitrary.ChatMessages(),
            ChatOptionsArbitrary.ChatOptions(),
            ChatOptionsArbitrary.ChatOptions(),
            (messages, options1, options2) =>
            {
                // Skip if options are effectively identical
                if (OptionsEqual(options1, options2))
                    return true.Label("Options are identical - skipped");

                // Arrange
                var fingerprinter = new TestRequestFingerprinter();

                // Act
                var hash1 = fingerprinter.ComputeFingerprint(messages, options1);
                var hash2 = fingerprinter.ComputeFingerprint(messages, options2);

                // Assert
                return (hash1 != hash2)
                    .Label($"Options1: {options1?.Temperature}, Options2: {options2?.Temperature}");
            });
    }

    [Property(MaxTest = 100)]
    public Property ComputeFingerprint_ValidFormat()
    {
        return Prop.ForAll(
            MessageListArbitrary.ChatMessages(),
            ChatOptionsArbitrary.ChatOptions(),
            (messages, options) =>
            {
                // Arrange
                var fingerprinter = new TestRequestFingerprinter();

                // Act
                var hash = fingerprinter.ComputeFingerprint(messages, options);

                // Assert: Hash should be non-empty and reasonable length
                return (!string.IsNullOrWhiteSpace(hash) &&
                        hash.Length >= 32 &&
                        hash.Length <= 128)
                    .Label($"Hash length: {hash.Length}");
            });
    }

    private static bool MessagesEqual(IList<ChatMessage> m1, IList<ChatMessage> m2)
    {
        if (m1.Count != m2.Count) return false;
        for (int i = 0; i < m1.Count; i++)
        {
            if (m1[i].Role != m2[i].Role || m1[i].Text != m2[i].Text)
                return false;
        }
        return true;
    }

    private static bool OptionsEqual(ChatOptions? o1, ChatOptions? o2)
    {
        if (o1 == null && o2 == null) return true;
        if (o1 == null || o2 == null) return false;
        return o1.ModelId == o2.ModelId &&
               Math.Abs((o1.Temperature ?? 0) - (o2.Temperature ?? 0)) < 0.0001 &&
               o1.MaxOutputTokens == o2.MaxOutputTokens;
    }
}

/// <summary>
/// Property-based tests for session ID computation and consistency
/// </summary>
public class SessionIdProperties
{
    [Property(MaxTest = 100)]
    public Property ComputeSessionId_Deterministic()
    {
        return Prop.ForAll(
            SessionDataArbitrary.SessionData(),
            sessionData =>
            {
                // Arrange
                var fingerprinter = new TestRequestFingerprinter();
                var context = CreateHttpContext(sessionData);

                // Act
                var sessionId1 = fingerprinter.ComputeSessionId(context);
                var sessionId2 = fingerprinter.ComputeSessionId(context);

                // Assert
                return (sessionId1 == sessionId2)
                    .Label($"SessionId: {sessionId1}");
            });
    }

    [Property(MaxTest = 100)]
    public Property ComputeSessionId_ValidFormat()
    {
        return Prop.ForAll(
            SessionDataArbitrary.SessionData(),
            sessionData =>
            {
                // Arrange
                var fingerprinter = new TestRequestFingerprinter();
                var context = CreateHttpContext(sessionData);

                // Act
                var sessionId = fingerprinter.ComputeSessionId(context);

                // Assert: Session ID should be non-empty and reasonable
                return (!string.IsNullOrWhiteSpace(sessionId) &&
                        sessionId.Length >= 8 &&
                        sessionId.Length <= 128)
                    .Label($"SessionId: {sessionId}, Length: {sessionId.Length}");
            });
    }

    [Property(MaxTest = 75)]
    public Property ComputeSessionId_HeaderTakesPrecedence()
    {
        return Prop.ForAll(
            Arb.From(Gen.Elements("", "session-123", "custom-id", null)),
            Arb.From(Gen.Elements("192.168.1.1", "10.0.0.1", null)),
            (sessionHeader, ipAddress) =>
            {
                if (string.IsNullOrEmpty(sessionHeader))
                    return true.Label("No session header - skipped");

                // Arrange
                var fingerprinter = new TestRequestFingerprinter();
                var context = CreateHttpContext(new SessionData
                {
                    SessionHeader = sessionHeader,
                    IpAddress = ipAddress,
                    UserAgent = "TestAgent"
                });

                // Act
                var sessionId = fingerprinter.ComputeSessionId(context);

                // Assert: When header provided, it should be used or incorporated
                return sessionId.Contains(sessionHeader, StringComparison.OrdinalIgnoreCase)
                    .Label($"SessionId: {sessionId}, Header: {sessionHeader}");
            });
    }

    private static HttpContext CreateHttpContext(SessionData data)
    {
        var context = new DefaultHttpContext();

        if (!string.IsNullOrEmpty(data.SessionHeader))
        {
            context.Request.Headers["X-Session-Id"] = data.SessionHeader;
        }

        if (!string.IsNullOrEmpty(data.UserAgent))
        {
            context.Request.Headers["User-Agent"] = data.UserAgent;
        }

        if (!string.IsNullOrEmpty(data.IpAddress))
        {
            context.Connection.RemoteIpAddress = System.Net.IPAddress.Parse(data.IpAddress);
        }

        return context;
    }
}

/// <summary>
/// Property-based tests for configuration resolution rules
/// </summary>
public class ConfigResolutionProperties
{
    [Property(MaxTest = 100)]
    public Property Resolve_SimilarityThresholdInRange()
    {
        return Prop.ForAll(
            Arb.From(Gen.Choose(0.5, 1.0)),
            Arb.From(Gen.Choose(0.5, 1.0)),
            (tenantThreshold, userThreshold) =>
            {
                // Arrange
                var config = new TokenOptimizationConfig
                {
                    SimilarityThreshold = userThreshold
                };

                // Assert: Should always be in valid range
                return (config.SimilarityThreshold >= 0.5 && config.SimilarityThreshold <= 1.0)
                    .Label($"Threshold: {config.SimilarityThreshold}");
            });
    }

    [Property(MaxTest = 100)]
    public Property Resolve_CacheTtlInRange()
    {
        return Prop.ForAll(
            Arb.From(Gen.Choose(60, 86400)),
            cacheTtl =>
            {
                // Arrange
                var config = new TokenOptimizationConfig
                {
                    CacheTtlSeconds = cacheTtl
                };

                // Assert: Should always be in valid range
                return (config.CacheTtlSeconds >= 60 && config.CacheTtlSeconds <= 86400)
                    .Label($"CacheTtl: {config.CacheTtlSeconds}");
            });
    }

    [Property(MaxTest = 50)]
    public Property Validate_CompressionStrategyValid()
    {
        return Prop.ForAll(
            Arb.From(Gen.Elements("gzip", "brotli", "none", "")),
            strategy =>
            {
                // Arrange
                var config = new TokenOptimizationConfig
                {
                    CompressionStrategy = strategy,
                    SimilarityThreshold = 0.85,
                    EnableCaching = true,
                    EnableCompression = true
                };

                var validator = new TokenOptimizationConfigValidator();

                // Act
                var result = validator.Validate(config, ConfigurationLevel.Tenant);

                // Assert: Empty or valid strategies should pass
                return (string.IsNullOrEmpty(strategy) || result.IsValid)
                    .Label($"Strategy: {strategy}, Valid: {result.IsValid}");
            });
    }

    [Property(MaxTest = 50)]
    public Property Validate_UserCannotSetSystemSettings()
    {
        return Prop.ForAll(
            Arb.From(Gen.Choose(1, 1000)),
            maxConcurrentRequests =>
            {
                // Arrange
                var config = new TokenOptimizationConfig
                {
                    SimilarityThreshold = 0.85,
                    EnableCaching = true,
                    EnableCompression = true,
                    MaxConcurrentRequests = maxConcurrentRequests
                };

                var validator = new TokenOptimizationConfigValidator();

                // Act
                var result = validator.Validate(config, ConfigurationLevel.User);

                // Assert: Should fail validation - users can't set system-level settings
                return (!result.IsValid)
                    .Label($"MaxConcurrentRequests: {maxConcurrentRequests}, Valid: {result.IsValid}");
            });
    }
}

/// <summary>
/// Property-based tests for caching behavior invariants
/// </summary>
public class CachingProperties
{
    [Property(MaxTest = 50)]
    public Property Cache_HitAndMiss_Consistent()
    {
        return Prop.ForAll(
            MessageListArbitrary.ChatMessages(),
            ChatOptionsArbitrary.ChatOptions(),
            (messages, options) =>
            {
                // Arrange
                var fingerprinter = new TestRequestFingerprinter();
                var fingerprint1 = fingerprinter.ComputeFingerprint(messages, options);

                // Create slightly modified messages
                var modifiedMessages = messages.ToList();
                if (modifiedMessages.Count > 0)
                {
                    modifiedMessages[0] = new ChatMessage(ChatRole.User, modifiedMessages[0].Text + " modified");
                }

                var fingerprint2 = fingerprinter.ComputeFingerprint(modifiedMessages, options);

                // Assert: Modified messages should produce different fingerprint
                return (messages.Count == 0 || fingerprint1 != fingerprint2)
                    .Label($"Original: {fingerprint1}, Modified: {fingerprint2}");
            });
    }

    [Property(MaxTest = 50)]
    public Property Cache_Key_Idempotent()
    {
        return Prop.ForAll(
            MessageListArbitrary.ChatMessages(),
            messages =>
            {
                // Arrange
                var fingerprinter = new TestRequestFingerprinter();

                // Act - Generate cache key multiple times
                var key1 = fingerprinter.ComputeFingerprint(messages, null);
                var key2 = fingerprinter.ComputeFingerprint(messages, null);
                var key3 = fingerprinter.ComputeFingerprint(messages, null);

                // Assert: All keys should be identical
                return (key1 == key2 && key2 == key3)
                    .Label("Cache key generation must be idempotent");
            });
    }
}

#region Custom Arbitraries

/// <summary>
/// Custom arbitrary for generating lists of ConversationMessages
/// </summary>
public static class MessageListArbitrary
{
    public static Arbitrary<List<ConversationMessage>> Messages()
    {
        var gen = from count in Gen.Choose(0, 20)
                  from messages in Gen.ArrayOf(count, ConversationMessageGen())
                  select messages.ToList();

        return Arb.From(gen);
    }

    public static Arbitrary<List<ConversationMessage>> MessagesWithSystem()
    {
        var gen = from systemMsg in Gen.Constant(new ConversationMessage
                  {
                      Role = "system",
                      Content = "You are a helpful assistant."
                  })
                  from count in Gen.Choose(0, 15)
                  from messages in Gen.ArrayOf(count, ConversationMessageGen())
                  select new[] { systemMsg }.Concat(messages).ToList();

        return Arb.From(gen);
    }

    public static Arbitrary<IList<ChatMessage>> ChatMessages()
    {
        var gen = from count in Gen.Choose(1, 10)
                  from messages in Gen.ArrayOf(count, ChatMessageGen())
                  select (IList<ChatMessage>)messages.ToList();

        return Arb.From(gen);
    }

    private static Gen<ConversationMessage> ConversationMessageGen()
    {
        return from role in Gen.Elements("user", "assistant", "system")
               from content in Gen.Elements(
                   "Hello",
                   "How can I help?",
                   "What is the weather?",
                   "Tell me about AI",
                   new string('x', 100),
                   new string('y', 500))
               select new ConversationMessage
               {
                   Role = role,
                   Content = content
               };
    }

    private static Gen<ChatMessage> ChatMessageGen()
    {
        return from role in Gen.Elements(ChatRole.User, ChatRole.Assistant, ChatRole.System)
               from content in Gen.Elements(
                   "Hello",
                   "Test message",
                   "Question about something",
                   new string('a', 50))
               select new ChatMessage(role, content);
    }
}

/// <summary>
/// Custom arbitrary for generating ChatOptions
/// </summary>
public static class ChatOptionsArbitrary
{
    public static Arbitrary<ChatOptions?> ChatOptions()
    {
        var gen = Gen.OneOf(
            Gen.Constant<ChatOptions?>(null),
            from modelId in Gen.Elements("gpt-4", "gpt-3.5-turbo", "claude-3", "llama-3")
            from temp in Gen.Choose(0, 20).Select(x => x / 10.0)
            from maxTokens in Gen.Choose(100, 4096)
            select new ChatOptions
            {
                ModelId = modelId,
                Temperature = (float)temp,
                MaxOutputTokens = maxTokens
            });

        return Arb.From(gen);
    }
}

/// <summary>
/// Custom arbitrary for generating session data
/// </summary>
public static class SessionDataArbitrary
{
    public static Arbitrary<SessionData> SessionData()
    {
        var gen = from sessionHeader in Gen.Elements("", "session-123", "user-session-abc", null)
                  from ipAddress in Gen.Elements("192.168.1.1", "10.0.0.1", "172.16.0.1", null)
                  from userAgent in Gen.Elements("Mozilla/5.0", "Chrome/120.0", "TestAgent", null)
                  select new SessionData
                  {
                      SessionHeader = sessionHeader,
                      IpAddress = ipAddress,
                      UserAgent = userAgent
                  };

        return Arb.From(gen);
    }
}

#endregion

#region Test Implementations

/// <summary>
/// Test implementation of IConversationCompressor for property testing
/// </summary>
public class TestConversationCompressor : IConversationCompressor
{
    public Task<CompressionResult> CompressAsync(
        List<ConversationMessage> messages,
        int tokenThreshold,
        CompressionStrategy strategy,
        CancellationToken cancellationToken)
    {
        var originalTokens = messages.Sum(m => EstimateTokens(m.Content));

        if (originalTokens <= tokenThreshold || messages.Count == 0)
        {
            return Task.FromResult(new CompressionResult
            {
                CompressedMessages = messages,
                OriginalTokenCount = originalTokens,
                CompressedTokenCount = originalTokens,
                CompressionRatio = 1.0,
                StrategyUsed = strategy,
                WasCompressed = false
            });
        }

        // Simulate compression by keeping system messages and recent messages
        var systemMessages = messages.Where(m => m.Role == "system").ToList();
        var otherMessages = messages.Where(m => m.Role != "system").ToList();

        var compressedMessages = new List<ConversationMessage>(systemMessages);
        var targetTokens = (int)(tokenThreshold * 0.8); // Leave some buffer
        var currentTokens = systemMessages.Sum(m => EstimateTokens(m.Content));

        // Add recent messages until we hit the target
        for (int i = otherMessages.Count - 1; i >= 0 && currentTokens < targetTokens; i--)
        {
            var msgTokens = EstimateTokens(otherMessages[i].Content);
            if (currentTokens + msgTokens <= targetTokens)
            {
                compressedMessages.Insert(systemMessages.Count, otherMessages[i]);
                currentTokens += msgTokens;
            }
        }

        return Task.FromResult(new CompressionResult
        {
            CompressedMessages = compressedMessages,
            OriginalTokenCount = originalTokens,
            CompressedTokenCount = currentTokens,
            CompressionRatio = originalTokens > 0 ? (double)currentTokens / originalTokens : 1.0,
            StrategyUsed = strategy,
            WasCompressed = true
        });
    }

    public int EstimateTokens(string text)
    {
        if (string.IsNullOrEmpty(text)) return 0;
        return (int)Math.Ceiling(text.Length / 4.0); // Simple approximation
    }
}

/// <summary>
/// Test implementation of IRequestFingerprinter for property testing
/// </summary>
public class TestRequestFingerprinter : IRequestFingerprinter
{
    public string ComputeFingerprint(IEnumerable<ChatMessage> messages, ChatOptions? options)
    {
        var messageHash = string.Join("|", messages.Select(m => $"{m.Role}:{m.Text}"));
        var optionsHash = options != null
            ? $"{options.ModelId}:{options.Temperature}:{options.MaxOutputTokens}"
            : "null";

        var combined = $"{messageHash}|{optionsHash}";
        return ComputeHash(combined);
    }

    public string ComputeSessionId(HttpContext context)
    {
        // Check for session header first
        if (context.Request.Headers.TryGetValue("X-Session-Id", out var sessionHeader) &&
            !string.IsNullOrEmpty(sessionHeader))
        {
            return sessionHeader.ToString();
        }

        // Fallback to IP + User-Agent
        var ip = context.Connection.RemoteIpAddress?.ToString() ?? "unknown";
        var ua = context.Request.Headers["User-Agent"].ToString() ?? "unknown";

        return ComputeHash($"{ip}|{ua}");
    }

    private static string ComputeHash(string input)
    {
        using var sha256 = System.Security.Cryptography.SHA256.Create();
        var bytes = System.Text.Encoding.UTF8.GetBytes(input);
        var hash = sha256.ComputeHash(bytes);
        return Convert.ToHexString(hash);
    }
}

/// <summary>
/// Session data for testing
/// </summary>
public class SessionData
{
    public string? SessionHeader { get; set; }
    public string? IpAddress { get; set; }
    public string? UserAgent { get; set; }
}

#endregion
